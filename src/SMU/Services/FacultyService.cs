using Microsoft.EntityFrameworkCore;
using SMU.Data;
using SMU.Data.Entities;
using SMU.Services.DTOs;

namespace SMU.Services;

/// <summary>
/// Service interface for faculty management
/// </summary>
public interface IFacultyService
{
    Task<PagedResult<FacultyListDto>> GetAllAsync(FacultyFilter filter, int page = 1, int pageSize = 25);
    Task<FacultyDetailDto?> GetByIdAsync(Guid id);
    Task<ServiceResult<Guid>> CreateAsync(CreateFacultyDto dto);
    Task<ServiceResult> UpdateAsync(Guid id, UpdateFacultyDto dto);
    Task<ServiceResult> DeleteAsync(Guid id);
    Task<List<FacultyListDto>> ExportAsync(FacultyFilter filter);
}

/// <summary>
/// Faculty management service implementation
/// </summary>
public class FacultyService : IFacultyService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FacultyService> _logger;

    public FacultyService(
        ApplicationDbContext context,
        ILogger<FacultyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResult<FacultyListDto>> GetAllAsync(FacultyFilter filter, int page = 1, int pageSize = 25)
    {
        var query = _context.Faculties
            .Include(f => f.Dean)
            .Include(f => f.Programs)
                .ThenInclude(p => p.Groups)
                    .ThenInclude(g => g.Students)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchLower = filter.Search.ToLower();
            query = query.Where(f =>
                f.Name.ToLower().Contains(searchLower) ||
                f.Code.ToLower().Contains(searchLower));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(f => f.IsActive == filter.IsActive.Value);
        }

        var totalCount = await query.CountAsync();

        var faculties = await query
            .OrderBy(f => f.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FacultyListDto
            {
                Id = f.Id,
                Name = f.Name,
                Code = f.Code,
                DeanName = f.Dean != null ? $"{f.Dean.FirstName} {f.Dean.LastName}" : null,
                ProgramsCount = f.Programs.Count,
                StudentsCount = f.Programs.SelectMany(p => p.Groups).SelectMany(g => g.Students).Count(),
                IsActive = f.IsActive
            })
            .ToListAsync();

        return new PagedResult<FacultyListDto>
        {
            Items = faculties,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<FacultyDetailDto?> GetByIdAsync(Guid id)
    {
        var faculty = await _context.Faculties
            .Include(f => f.Dean)
            .Include(f => f.Programs)
                .ThenInclude(p => p.Groups)
                    .ThenInclude(g => g.Students)
            .Include(f => f.Professors)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (faculty == null)
            return null;

        return new FacultyDetailDto
        {
            Id = faculty.Id,
            Name = faculty.Name,
            Code = faculty.Code,
            Description = faculty.Description,
            DeanId = faculty.DeanId,
            DeanName = faculty.Dean != null ? $"{faculty.Dean.FirstName} {faculty.Dean.LastName}" : null,
            DeanEmail = faculty.Dean?.Email,
            IsActive = faculty.IsActive,
            ProgramsCount = faculty.Programs.Count,
            ProfessorsCount = faculty.Professors.Count,
            StudentsCount = faculty.Programs.SelectMany(p => p.Groups).SelectMany(g => g.Students).Count(),
            CreatedAt = faculty.CreatedAt,
            UpdatedAt = faculty.UpdatedAt
        };
    }

    public async Task<ServiceResult<Guid>> CreateAsync(CreateFacultyDto dto)
    {
        try
        {
            // Verify code is unique
            if (await _context.Faculties.AnyAsync(f => f.Code == dto.Code))
            {
                return ServiceResult<Guid>.Failed("Codul facultății este deja folosit.");
            }

            // Verify dean exists if provided
            if (dto.DeanId.HasValue)
            {
                var dean = await _context.Users.FindAsync(dto.DeanId.Value);
                if (dean == null || dean.Role != UserRole.Dean)
                {
                    return ServiceResult<Guid>.Failed("Decanul selectat nu există sau nu are rolul de decan.");
                }
            }

            var faculty = new Faculty
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Code = dto.Code,
                Description = dto.Description,
                DeanId = dto.DeanId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Faculties.Add(faculty);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Faculty created successfully: {FacultyId} - {Name}", faculty.Id, dto.Name);

            return ServiceResult<Guid>.Success(faculty.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating faculty");
            return ServiceResult<Guid>.Failed("Eroare la crearea facultății. Vă rugăm încercați din nou.");
        }
    }

    public async Task<ServiceResult> UpdateAsync(Guid id, UpdateFacultyDto dto)
    {
        try
        {
            var faculty = await _context.Faculties.FindAsync(id);

            if (faculty == null)
            {
                return ServiceResult.Failed("Facultatea nu a fost găsită.");
            }

            // Verify code is unique (excluding current faculty)
            if (await _context.Faculties.AnyAsync(f => f.Code == dto.Code && f.Id != id))
            {
                return ServiceResult.Failed("Codul facultății este deja folosit.");
            }

            // Verify dean exists if provided
            if (dto.DeanId.HasValue)
            {
                var dean = await _context.Users.FindAsync(dto.DeanId.Value);
                if (dean == null || dean.Role != UserRole.Dean)
                {
                    return ServiceResult.Failed("Decanul selectat nu există sau nu are rolul de decan.");
                }
            }

            faculty.Name = dto.Name;
            faculty.Code = dto.Code;
            faculty.Description = dto.Description;
            faculty.DeanId = dto.DeanId;
            faculty.IsActive = dto.IsActive;
            faculty.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Faculty updated successfully: {FacultyId}", id);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating faculty {FacultyId}", id);
            return ServiceResult.Failed("Eroare la actualizarea facultății. Vă rugăm încercați din nou.");
        }
    }

    public async Task<ServiceResult> DeleteAsync(Guid id)
    {
        try
        {
            var faculty = await _context.Faculties
                .Include(f => f.Programs)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (faculty == null)
            {
                return ServiceResult.Failed("Facultatea nu a fost găsită.");
            }

            // Check if faculty has programs
            if (faculty.Programs.Any())
            {
                return ServiceResult.Failed("Nu se poate șterge facultatea. Există programe de studiu asociate.");
            }

            // Soft delete - set status to Inactive
            faculty.IsActive = false;
            faculty.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Faculty soft deleted: {FacultyId}", id);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting faculty {FacultyId}", id);
            return ServiceResult.Failed("Eroare la ștergerea facultății. Vă rugăm încercați din nou.");
        }
    }

    public async Task<List<FacultyListDto>> ExportAsync(FacultyFilter filter)
    {
        var query = _context.Faculties
            .Include(f => f.Dean)
            .Include(f => f.Programs)
                .ThenInclude(p => p.Groups)
                    .ThenInclude(g => g.Students)
            .AsQueryable();

        // Apply same filters as GetAllAsync
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchLower = filter.Search.ToLower();
            query = query.Where(f =>
                f.Name.ToLower().Contains(searchLower) ||
                f.Code.ToLower().Contains(searchLower));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(f => f.IsActive == filter.IsActive.Value);
        }

        return await query
            .OrderBy(f => f.Name)
            .Select(f => new FacultyListDto
            {
                Id = f.Id,
                Name = f.Name,
                Code = f.Code,
                DeanName = f.Dean != null ? $"{f.Dean.FirstName} {f.Dean.LastName}" : null,
                ProgramsCount = f.Programs.Count,
                StudentsCount = f.Programs.SelectMany(p => p.Groups).SelectMany(g => g.Students).Count(),
                IsActive = f.IsActive
            })
            .ToListAsync();
    }
}
