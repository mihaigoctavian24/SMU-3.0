using Microsoft.EntityFrameworkCore;
using SMU.Data;
using SMU.Data.Entities;
using SMU.Services.DTOs;

namespace SMU.Services;

/// <summary>
/// Service interface for group management
/// </summary>
public interface IGroupService
{
    Task<PagedResult<GroupListDto>> GetAllAsync(GroupFilter filter, int page = 1, int pageSize = 25);
    Task<GroupDetailDto?> GetByIdAsync(Guid id);
    Task<ServiceResult<Guid>> CreateAsync(CreateGroupDto dto);
    Task<ServiceResult> UpdateAsync(Guid id, UpdateGroupDto dto);
    Task<ServiceResult> DeleteAsync(Guid id);
    Task<List<GroupListDto>> ExportAsync(GroupFilter filter);
}

/// <summary>
/// Group management service implementation
/// </summary>
public class GroupService : IGroupService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GroupService> _logger;

    public GroupService(
        ApplicationDbContext context,
        ILogger<GroupService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResult<GroupListDto>> GetAllAsync(GroupFilter filter, int page = 1, int pageSize = 25)
    {
        var query = _context.Groups
            .Include(g => g.Program)
                .ThenInclude(p => p.Faculty)
            .Include(g => g.Students)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchLower = filter.Search.ToLower();
            query = query.Where(g =>
                g.Name.ToLower().Contains(searchLower));
        }

        if (filter.ProgramId.HasValue)
        {
            query = query.Where(g => g.ProgramId == filter.ProgramId.Value);
        }

        if (filter.FacultyId.HasValue)
        {
            query = query.Where(g => g.Program.FacultyId == filter.FacultyId.Value);
        }

        if (filter.Year.HasValue)
        {
            query = query.Where(g => g.Year == filter.Year.Value);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(g => g.IsActive == filter.IsActive.Value);
        }

        var totalCount = await query.CountAsync();

        var groups = await query
            .OrderBy(g => g.Program.Faculty.Name)
            .ThenBy(g => g.Program.Name)
            .ThenBy(g => g.Year)
            .ThenBy(g => g.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new GroupListDto
            {
                Id = g.Id,
                Name = g.Name,
                Year = g.Year,
                ProgramName = g.Program.Name,
                ProgramId = g.ProgramId,
                FacultyName = g.Program.Faculty.Name,
                StudentsCount = g.Students.Count,
                MaxStudents = g.MaxStudents,
                IsActive = g.IsActive
            })
            .ToListAsync();

        return new PagedResult<GroupListDto>
        {
            Items = groups,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<GroupDetailDto?> GetByIdAsync(Guid id)
    {
        var group = await _context.Groups
            .Include(g => g.Program)
                .ThenInclude(p => p.Faculty)
            .Include(g => g.Students)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null)
            return null;

        return new GroupDetailDto
        {
            Id = group.Id,
            Name = group.Name,
            Year = group.Year,
            ProgramId = group.ProgramId,
            ProgramName = group.Program.Name,
            FacultyName = group.Program.Faculty.Name,
            MaxStudents = group.MaxStudents,
            StudentsCount = group.Students.Count,
            IsActive = group.IsActive,
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt
        };
    }

    public async Task<ServiceResult<Guid>> CreateAsync(CreateGroupDto dto)
    {
        try
        {
            // Verify program exists
            var program = await _context.Programs.FindAsync(dto.ProgramId);
            if (program == null)
            {
                return ServiceResult<Guid>.Failed("Programul de studiu selectat nu există.");
            }

            // Verify name is unique within program and year
            if (await _context.Groups.AnyAsync(g => g.Name == dto.Name && g.ProgramId == dto.ProgramId && g.Year == dto.Year))
            {
                return ServiceResult<Guid>.Failed("Există deja o grupă cu acest nume pentru programul și anul selectat.");
            }

            var group = new Group
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                ProgramId = dto.ProgramId,
                Year = dto.Year,
                MaxStudents = dto.MaxStudents,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Group created successfully: {GroupId} - {Name}", group.Id, dto.Name);

            return ServiceResult<Guid>.Success(group.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group");
            return ServiceResult<Guid>.Failed("Eroare la crearea grupei. Vă rugăm încercați din nou.");
        }
    }

    public async Task<ServiceResult> UpdateAsync(Guid id, UpdateGroupDto dto)
    {
        try
        {
            var group = await _context.Groups.FindAsync(id);

            if (group == null)
            {
                return ServiceResult.Failed("Grupa nu a fost găsită.");
            }

            // Verify name is unique (excluding current group)
            if (await _context.Groups.AnyAsync(g => g.Name == dto.Name && g.ProgramId == group.ProgramId && g.Year == dto.Year && g.Id != id))
            {
                return ServiceResult.Failed("Există deja o grupă cu acest nume pentru programul și anul selectat.");
            }

            group.Name = dto.Name;
            group.Year = dto.Year;
            group.MaxStudents = dto.MaxStudents;
            group.IsActive = dto.IsActive;
            group.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Group updated successfully: {GroupId}", id);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating group {GroupId}", id);
            return ServiceResult.Failed("Eroare la actualizarea grupei. Vă rugăm încercați din nou.");
        }
    }

    public async Task<ServiceResult> DeleteAsync(Guid id)
    {
        try
        {
            var group = await _context.Groups
                .Include(g => g.Students)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
            {
                return ServiceResult.Failed("Grupa nu a fost găsită.");
            }

            // Check if group has students
            if (group.Students.Any())
            {
                return ServiceResult.Failed("Nu se poate șterge grupa. Există studenți înscriși în această grupă.");
            }

            // Soft delete - set status to Inactive
            group.IsActive = false;
            group.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Group soft deleted: {GroupId}", id);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting group {GroupId}", id);
            return ServiceResult.Failed("Eroare la ștergerea grupei. Vă rugăm încercați din nou.");
        }
    }

    public async Task<List<GroupListDto>> ExportAsync(GroupFilter filter)
    {
        var query = _context.Groups
            .Include(g => g.Program)
                .ThenInclude(p => p.Faculty)
            .Include(g => g.Students)
            .AsQueryable();

        // Apply same filters as GetAllAsync
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchLower = filter.Search.ToLower();
            query = query.Where(g =>
                g.Name.ToLower().Contains(searchLower));
        }

        if (filter.ProgramId.HasValue)
        {
            query = query.Where(g => g.ProgramId == filter.ProgramId.Value);
        }

        if (filter.FacultyId.HasValue)
        {
            query = query.Where(g => g.Program.FacultyId == filter.FacultyId.Value);
        }

        if (filter.Year.HasValue)
        {
            query = query.Where(g => g.Year == filter.Year.Value);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(g => g.IsActive == filter.IsActive.Value);
        }

        return await query
            .OrderBy(g => g.Program.Faculty.Name)
            .ThenBy(g => g.Program.Name)
            .ThenBy(g => g.Year)
            .ThenBy(g => g.Name)
            .Select(g => new GroupListDto
            {
                Id = g.Id,
                Name = g.Name,
                Year = g.Year,
                ProgramName = g.Program.Name,
                ProgramId = g.ProgramId,
                FacultyName = g.Program.Faculty.Name,
                StudentsCount = g.Students.Count,
                MaxStudents = g.MaxStudents,
                IsActive = g.IsActive
            })
            .ToListAsync();
    }
}
