using Microsoft.EntityFrameworkCore;
using SMU.Data;
using SMU.Data.Entities;
using SMU.Services.DTOs;

namespace SMU.Services;

public class ScheduleService : IScheduleService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ScheduleService> _logger;

    private static readonly Dictionary<int, string> DayNames = new()
    {
        { 1, "Luni" },
        { 2, "Marți" },
        { 3, "Miercuri" },
        { 4, "Joi" },
        { 5, "Vineri" },
        { 6, "Sâmbătă" },
        { 7, "Duminică" }
    };

    private static readonly Dictionary<string, string> TypeLabels = new()
    {
        { "Curs", "Curs" },
        { "Seminar", "Seminar" },
        { "Laborator", "Laborator" }
    };

    public ScheduleService(ApplicationDbContext context, ILogger<ScheduleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ScheduleEntryDto>> GetByGroupAsync(Guid groupId, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        var query = _context.ScheduleEntries
            .Include(s => s.Course)
                .ThenInclude(c => c.Professor)
                .ThenInclude(p => p!.User)
            .Include(s => s.Group)
            .Where(s => s.GroupId == groupId);

        var entries = await query.ToListAsync();

        return MapToScheduleEntryDtos(entries);
    }

    public async Task<List<ScheduleEntryDto>> GetByProfessorAsync(Guid professorId, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        var query = _context.ScheduleEntries
            .Include(s => s.Course)
                .ThenInclude(c => c.Professor)
                .ThenInclude(p => p!.User)
            .Include(s => s.Group)
            .Where(s => s.Course.ProfessorId == professorId);

        var entries = await query.ToListAsync();

        return MapToScheduleEntryDtos(entries);
    }

    public async Task<List<ScheduleEntryDto>> GetByStudentAsync(Guid studentId, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        var student = await _context.Students
            .Include(s => s.Group)
            .FirstOrDefaultAsync(s => s.Id == studentId);

        if (student?.GroupId == null)
            return new List<ScheduleEntryDto>();

        return await GetByGroupAsync(student.GroupId.Value, startDate, endDate);
    }

    public async Task<ScheduleEntryDto?> GetByIdAsync(Guid id)
    {
        var entry = await _context.ScheduleEntries
            .Include(s => s.Course)
                .ThenInclude(c => c.Professor)
                .ThenInclude(p => p!.User)
            .Include(s => s.Group)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (entry == null)
            return null;

        return MapToScheduleEntryDto(entry);
    }

    public async Task<List<WeeklyScheduleDto>> GetWeeklyScheduleByGroupAsync(Guid groupId)
    {
        var entries = await GetByGroupAsync(groupId);
        return BuildWeeklySchedule(entries);
    }

    public async Task<List<WeeklyScheduleDto>> GetWeeklyScheduleByProfessorAsync(Guid professorId)
    {
        var entries = await GetByProfessorAsync(professorId);
        return BuildWeeklySchedule(entries);
    }

    public async Task<List<WeeklyScheduleDto>> GetWeeklyScheduleByStudentAsync(Guid studentId)
    {
        var entries = await GetByStudentAsync(studentId);
        return BuildWeeklySchedule(entries);
    }

    public async Task<ServiceResult<Guid>> CreateAsync(CreateScheduleEntryDto dto)
    {
        try
        {
            // Validate course exists
            var courseExists = await _context.Courses
                .AnyAsync(c => c.Id == dto.CourseId && c.IsActive);

            if (!courseExists)
            {
                return ServiceResult<Guid>.Failed("Cursul selectat nu există sau este inactiv.");
            }

            // Validate group exists
            var groupExists = await _context.Groups
                .AnyAsync(g => g.Id == dto.GroupId && g.IsActive);

            if (!groupExists)
            {
                return ServiceResult<Guid>.Failed("Grupa selectată nu există sau este inactivă.");
            }

            // Validate time range
            if (dto.EndTime <= dto.StartTime)
            {
                return ServiceResult<Guid>.Failed("Ora de sfârșit trebuie să fie după ora de început.");
            }

            // Validate day of week
            if (dto.DayOfWeek < 1 || dto.DayOfWeek > 7)
            {
                return ServiceResult<Guid>.Failed("Ziua săptămânii trebuie să fie între 1 (Luni) și 7 (Duminică).");
            }

            // Check for conflicts
            var conflict = await CheckConflictAsync(null, dto.DayOfWeek, dto.StartTime, dto.EndTime, dto.Room);
            if (conflict.HasConflict)
            {
                return ServiceResult<Guid>.Failed(conflict.ConflictMessage);
            }

            var entry = new ScheduleEntry
            {
                Id = Guid.NewGuid(),
                CourseId = dto.CourseId,
                GroupId = dto.GroupId,
                DayOfWeek = dto.DayOfWeek,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Room = dto.Room,
                Type = dto.Type,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ScheduleEntries.Add(entry);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Schedule entry created: {ScheduleId} for Course {CourseId}, Group {GroupId}",
                entry.Id, entry.CourseId, entry.GroupId);

            return ServiceResult<Guid>.Success(entry.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating schedule entry");
            return ServiceResult<Guid>.Failed("A apărut o eroare la crearea intrării în orar.");
        }
    }

    public async Task<ServiceResult> UpdateAsync(Guid id, UpdateScheduleEntryDto dto)
    {
        try
        {
            var entry = await _context.ScheduleEntries.FindAsync(id);

            if (entry == null)
            {
                return ServiceResult.Failed("Intrarea în orar nu a fost găsită.");
            }

            // Validate time range
            if (dto.EndTime <= dto.StartTime)
            {
                return ServiceResult.Failed("Ora de sfârșit trebuie să fie după ora de început.");
            }

            // Validate day of week
            if (dto.DayOfWeek < 1 || dto.DayOfWeek > 7)
            {
                return ServiceResult.Failed("Ziua săptămânii trebuie să fie între 1 (Luni) și 7 (Duminică).");
            }

            // Check for conflicts (excluding current entry)
            var conflict = await CheckConflictAsync(id, dto.DayOfWeek, dto.StartTime, dto.EndTime, dto.Room);
            if (conflict.HasConflict)
            {
                return ServiceResult.Failed(conflict.ConflictMessage);
            }

            entry.DayOfWeek = dto.DayOfWeek;
            entry.StartTime = dto.StartTime;
            entry.EndTime = dto.EndTime;
            entry.Room = dto.Room;
            entry.Type = dto.Type;
            entry.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Schedule entry updated: {ScheduleId}", id);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating schedule entry {ScheduleId}", id);
            return ServiceResult.Failed("A apărut o eroare la actualizarea intrării în orar.");
        }
    }

    public async Task<ServiceResult> DeleteAsync(Guid id)
    {
        try
        {
            var entry = await _context.ScheduleEntries.FindAsync(id);

            if (entry == null)
            {
                return ServiceResult.Failed("Intrarea în orar nu a fost găsită.");
            }

            _context.ScheduleEntries.Remove(entry);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Schedule entry deleted: {ScheduleId}", id);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting schedule entry {ScheduleId}", id);
            return ServiceResult.Failed("A apărut o eroare la ștergerea intrării în orar.");
        }
    }

    public async Task<ServiceResult> BulkCreateAsync(List<CreateScheduleEntryDto> entries)
    {
        try
        {
            if (!entries.Any())
            {
                return ServiceResult.Failed("Nu există intrări de creat.");
            }

            var scheduleEntries = new List<ScheduleEntry>();
            var errors = new List<string>();

            foreach (var dto in entries)
            {
                // Validate course exists
                var courseExists = await _context.Courses
                    .AnyAsync(c => c.Id == dto.CourseId && c.IsActive);

                if (!courseExists)
                {
                    errors.Add($"Cursul {dto.CourseId} nu există sau este inactiv.");
                    continue;
                }

                // Validate group exists
                var groupExists = await _context.Groups
                    .AnyAsync(g => g.Id == dto.GroupId && g.IsActive);

                if (!groupExists)
                {
                    errors.Add($"Grupa {dto.GroupId} nu există sau este inactivă.");
                    continue;
                }

                // Validate time range
                if (dto.EndTime <= dto.StartTime)
                {
                    errors.Add($"Ora de sfârșit trebuie să fie după ora de început pentru {dto.CourseId}.");
                    continue;
                }

                // Check for conflicts
                var conflict = await CheckConflictAsync(null, dto.DayOfWeek, dto.StartTime, dto.EndTime, dto.Room);
                if (conflict.HasConflict)
                {
                    errors.Add(conflict.ConflictMessage);
                    continue;
                }

                scheduleEntries.Add(new ScheduleEntry
                {
                    Id = Guid.NewGuid(),
                    CourseId = dto.CourseId,
                    GroupId = dto.GroupId,
                    DayOfWeek = dto.DayOfWeek,
                    StartTime = dto.StartTime,
                    EndTime = dto.EndTime,
                    Room = dto.Room,
                    Type = dto.Type,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            if (errors.Any())
            {
                return ServiceResult.Failed($"Erori la crearea unor intrări: {string.Join("; ", errors)}");
            }

            _context.ScheduleEntries.AddRange(scheduleEntries);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Bulk created {Count} schedule entries", scheduleEntries.Count);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk creating schedule entries");
            return ServiceResult.Failed("A apărut o eroare la crearea în masă a intrărilor în orar.");
        }
    }

    public async Task<ScheduleConflictDto> CheckConflictAsync(Guid? excludeId, int dayOfWeek, TimeOnly startTime, TimeOnly endTime, string? room)
    {
        if (string.IsNullOrWhiteSpace(room))
        {
            return new ScheduleConflictDto { HasConflict = false };
        }

        var query = _context.ScheduleEntries
            .Include(s => s.Course)
            .Include(s => s.Group)
            .Where(s => s.DayOfWeek == dayOfWeek && s.Room == room);

        if (excludeId.HasValue)
        {
            query = query.Where(s => s.Id != excludeId.Value);
        }

        var existingEntries = await query.ToListAsync();

        foreach (var entry in existingEntries)
        {
            // Check for time overlap
            if ((startTime >= entry.StartTime && startTime < entry.EndTime) ||
                (endTime > entry.StartTime && endTime <= entry.EndTime) ||
                (startTime <= entry.StartTime && endTime >= entry.EndTime))
            {
                return new ScheduleConflictDto
                {
                    HasConflict = true,
                    ConflictMessage = $"Conflict: Sala {room} este ocupată {DayNames[dayOfWeek]} între {entry.StartTime:HH:mm} și {entry.EndTime:HH:mm} de cursul {entry.Course.Name} (Grupa {entry.Group.Name}).",
                    ConflictingEntry = MapToScheduleEntryDto(entry)
                };
            }
        }

        return new ScheduleConflictDto { HasConflict = false };
    }

    public async Task<List<GroupOptionDto>> GetGroupOptionsAsync()
    {
        return await _context.Groups
            .Include(g => g.Program)
            .Where(g => g.IsActive)
            .OrderBy(g => g.Program.Name)
            .ThenBy(g => g.Year)
            .ThenBy(g => g.Name)
            .Select(g => new GroupOptionDto
            {
                Id = g.Id,
                Name = g.Name,
                ProgramName = g.Program.Name,
                Year = g.Year
            })
            .ToListAsync();
    }

    public async Task<List<CourseOptionDto>> GetCourseOptionsAsync()
    {
        return await _context.Courses
            .Include(c => c.Professor)
                .ThenInclude(p => p!.User)
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CourseOptionDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                ProfessorName = c.Professor != null
                    ? $"{c.Professor.User.FirstName} {c.Professor.User.LastName}"
                    : "Neasignat"
            })
            .ToListAsync();
    }

    private List<ScheduleEntryDto> MapToScheduleEntryDtos(List<ScheduleEntry> entries)
    {
        return entries.Select(MapToScheduleEntryDto).ToList();
    }

    private ScheduleEntryDto MapToScheduleEntryDto(ScheduleEntry entry)
    {
        return new ScheduleEntryDto
        {
            Id = entry.Id,
            CourseId = entry.CourseId,
            CourseName = entry.Course.Name,
            CourseCode = entry.Course.Code,
            GroupId = entry.GroupId,
            GroupName = entry.Group.Name,
            ProfessorId = entry.Course.ProfessorId,
            ProfessorName = entry.Course.Professor != null
                ? $"{entry.Course.Professor.User.FirstName} {entry.Course.Professor.User.LastName}"
                : "Neasignat",
            DayOfWeek = entry.DayOfWeek,
            DayName = DayNames.ContainsKey(entry.DayOfWeek) ? DayNames[entry.DayOfWeek] : "Necunoscut",
            StartTime = entry.StartTime,
            EndTime = entry.EndTime,
            Room = entry.Room,
            Type = entry.Type,
            TypeLabel = TypeLabels.ContainsKey(entry.Type) ? TypeLabels[entry.Type] : entry.Type
        };
    }

    private List<WeeklyScheduleDto> BuildWeeklySchedule(List<ScheduleEntryDto> entries)
    {
        var weeklySchedule = new List<WeeklyScheduleDto>();

        for (int day = 1; day <= 5; day++) // Monday to Friday
        {
            weeklySchedule.Add(new WeeklyScheduleDto
            {
                DayOfWeek = day,
                DayName = DayNames[day],
                Entries = entries
                    .Where(e => e.DayOfWeek == day)
                    .OrderBy(e => e.StartTime)
                    .ToList()
            });
        }

        return weeklySchedule;
    }
}
