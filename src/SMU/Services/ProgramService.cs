using Microsoft.EntityFrameworkCore;
using SMU.Data;
using SMU.Data.Entities;
using SMU.Services.DTOs;

namespace SMU.Services;

/// <summary>
/// Service interface for study program management
/// </summary>
public interface IProgramService
{
    Task<PagedResult<StudyProgramListDto>> GetAllAsync(StudyProgramFilter filter, int page = 1, int pageSize = 25);
    Task<StudyProgramDetailDto?> GetByIdAsync(Guid id);
    Task<ServiceResult<Guid>> CreateAsync(CreateStudyProgramDto dto);
    Task<ServiceResult> UpdateAsync(Guid id, UpdateStudyProgramDto dto);
    Task<ServiceResult> DeleteAsync(Guid id);
    Task<List<StudyProgramListDto>> ExportAsync(StudyProgramFilter filter);
}

/// <summary>
/// Study program management service implementation
/// </summary>
public class ProgramService : IProgramService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProgramService> _logger;

    public ProgramService(
        ApplicationDbContext context,
        ILogger<ProgramService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResult<StudyProgramListDto>> GetAllAsync(StudyProgramFilter filter, int page = 1, int pageSize = 25)
    {
        var query = _context.Programs
            .Include(p => p.Faculty)
            .Include(p => p.Groups)
                .ThenInclude(g => g.Students)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchLower = filter.Search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(searchLower) ||
                p.Code.ToLower().Contains(searchLower));
        }

        if (filter.FacultyId.HasValue)
        {
            query = query.Where(p => p.FacultyId == filter.FacultyId.Value);
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(p => p.Type == filter.Type.Value);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == filter.IsActive.Value);
        }

        var totalCount = await query.CountAsync();

        var programs = await query
            .OrderBy(p => p.Faculty.Name)
            .ThenBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new StudyProgramListDto
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code,
                FacultyName = p.Faculty.Name,
                FacultyId = p.FacultyId,
                Type = p.Type,
                DurationYears = p.DurationYears,
                GroupsCount = p.Groups.Count,
                StudentsCount = p.Groups.SelectMany(g => g.Students).Count(),
                IsActive = p.IsActive
            })
            .ToListAsync();

        return new PagedResult<StudyProgramListDto>
        {
            Items = programs,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<StudyProgramDetailDto?> GetByIdAsync(Guid id)
    {
        var program = await _context.Programs
            .Include(p => p.Faculty)
            .Include(p => p.Groups)
                .ThenInclude(g => g.Students)
            .Include(p => p.Courses)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (program == null)
            return null;

        return new StudyProgramDetailDto
        {
            Id = program.Id,
            Name = program.Name,
            Code = program.Code,
            FacultyId = program.FacultyId,
            FacultyName = program.Faculty.Name,
            Type = program.Type,
            DurationYears = program.DurationYears,
            TotalCredits = program.TotalCredits,
            Description = program.Description,
            IsActive = program.IsActive,
            GroupsCount = program.Groups.Count,
            CoursesCount = program.Courses.Count,
            StudentsCount = program.Groups.SelectMany(g => g.Students).Count(),
            CreatedAt = program.CreatedAt,
            UpdatedAt = program.UpdatedAt
        };
    }

    public async Task<ServiceResult<Guid>> CreateAsync(CreateStudyProgramDto dto)
    {
        try
        {
            // Verify faculty exists
            var faculty = await _context.Faculties.FindAsync(dto.FacultyId);
            if (faculty == null)
            {
                return ServiceResult<Guid>.Failed("Facultatea selectată nu există.");
            }

            // Verify code is unique within faculty
            if (await _context.Programs.AnyAsync(p => p.Code == dto.Code && p.FacultyId == dto.FacultyId))
            {
                return ServiceResult<Guid>.Failed("Codul programului de studiu este deja folosit în această facultate.");
            }

            var program = new StudyProgram
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Code = dto.Code,
                FacultyId = dto.FacultyId,
                Type = dto.Type,
                DurationYears = dto.DurationYears,
                TotalCredits = dto.TotalCredits,
                Description = dto.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Programs.Add(program);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Study program created successfully: {ProgramId} - {Name}", program.Id, dto.Name);

            return ServiceResult<Guid>.Success(program.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating study program");
            return ServiceResult<Guid>.Failed("Eroare la crearea programului de studiu. Vă rugăm încercați din nou.");
        }
    }

    public async Task<ServiceResult> UpdateAsync(Guid id, UpdateStudyProgramDto dto)
    {
        try
        {
            var program = await _context.Programs.FindAsync(id);

            if (program == null)
            {
                return ServiceResult.Failed("Programul de studiu nu a fost găsit.");
            }

            // Verify code is unique (excluding current program)
            if (await _context.Programs.AnyAsync(p => p.Code == dto.Code && p.FacultyId == program.FacultyId && p.Id != id))
            {
                return ServiceResult.Failed("Codul programului de studiu este deja folosit în această facultate.");
            }

            program.Name = dto.Name;
            program.Code = dto.Code;
            program.Type = dto.Type;
            program.DurationYears = dto.DurationYears;
            program.TotalCredits = dto.TotalCredits;
            program.Description = dto.Description;
            program.IsActive = dto.IsActive;
            program.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Study program updated successfully: {ProgramId}", id);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating study program {ProgramId}", id);
            return ServiceResult.Failed("Eroare la actualizarea programului de studiu. Vă rugăm încercați din nou.");
        }
    }

    public async Task<ServiceResult> DeleteAsync(Guid id)
    {
        try
        {
            var program = await _context.Programs
                .Include(p => p.Groups)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (program == null)
            {
                return ServiceResult.Failed("Programul de studiu nu a fost găsit.");
            }

            // Check if program has groups
            if (program.Groups.Any())
            {
                return ServiceResult.Failed("Nu se poate șterge programul de studiu. Există grupe asociate.");
            }

            // Soft delete - set status to Inactive
            program.IsActive = false;
            program.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Study program soft deleted: {ProgramId}", id);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting study program {ProgramId}", id);
            return ServiceResult.Failed("Eroare la ștergerea programului de studiu. Vă rugăm încercați din nou.");
        }
    }

    public async Task<List<StudyProgramListDto>> ExportAsync(StudyProgramFilter filter)
    {
        var query = _context.Programs
            .Include(p => p.Faculty)
            .Include(p => p.Groups)
                .ThenInclude(g => g.Students)
            .AsQueryable();

        // Apply same filters as GetAllAsync
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchLower = filter.Search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(searchLower) ||
                p.Code.ToLower().Contains(searchLower));
        }

        if (filter.FacultyId.HasValue)
        {
            query = query.Where(p => p.FacultyId == filter.FacultyId.Value);
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(p => p.Type == filter.Type.Value);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == filter.IsActive.Value);
        }

        return await query
            .OrderBy(p => p.Faculty.Name)
            .ThenBy(p => p.Name)
            .Select(p => new StudyProgramListDto
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code,
                FacultyName = p.Faculty.Name,
                FacultyId = p.FacultyId,
                Type = p.Type,
                DurationYears = p.DurationYears,
                GroupsCount = p.Groups.Count,
                StudentsCount = p.Groups.SelectMany(g => g.Students).Count(),
                IsActive = p.IsActive
            })
            .ToListAsync();
    }
}
