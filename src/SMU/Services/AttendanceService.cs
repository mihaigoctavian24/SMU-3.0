using Microsoft.EntityFrameworkCore;
using SMU.Data;
using SMU.Data.Entities;

namespace SMU.Services;

/// <summary>
/// Service for managing student attendance records
/// </summary>
public interface IAttendanceService
{
    Task<List<AttendanceListDto>> GetByStudentAndCourseAsync(Guid studentId, Guid? courseId);
    Task<List<AttendanceRecordDto>> GetByCourseDateAsync(Guid courseId, DateOnly date);
    Task<AttendanceResult> MarkAttendanceAsync(MarkAttendanceDto dto, Guid recordedById);
    Task<AttendanceResult> BulkMarkAsync(BulkAttendanceDto dto, Guid recordedById);
    Task<AttendanceStatsDto> GetStatsAsync(Guid studentId, Guid? courseId);
    Task<CalendarMonthDto> GetMonthlyCalendarAsync(Guid studentId, int year, int month);
    Task<List<CourseDto>> GetProfessorCoursesAsync(Guid professorId);
    Task<List<StudentInCourseDto>> GetStudentsInCourseAsync(Guid courseId);
}

public class AttendanceService : IAttendanceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(ApplicationDbContext context, ILogger<AttendanceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<AttendanceListDto>> GetByStudentAndCourseAsync(Guid studentId, Guid? courseId)
    {
        var query = _context.Attendances
            .Include(a => a.Course)
            .Where(a => a.StudentId == studentId);

        if (courseId.HasValue)
        {
            query = query.Where(a => a.CourseId == courseId.Value);
        }

        var attendances = await query
            .OrderByDescending(a => a.Date)
            .Select(a => new AttendanceListDto
            {
                Id = a.Id,
                CourseName = a.Course.Name,
                Date = a.Date,
                Status = a.Status,
                Notes = a.Notes
            })
            .ToListAsync();

        return attendances;
    }

    public async Task<List<AttendanceRecordDto>> GetByCourseDateAsync(Guid courseId, DateOnly date)
    {
        var attendances = await _context.Attendances
            .Include(a => a.Student)
                .ThenInclude(s => s.User)
            .Where(a => a.CourseId == courseId && a.Date == date)
            .Select(a => new AttendanceRecordDto
            {
                StudentId = a.StudentId,
                StudentName = a.Student.User.FirstName + " " + a.Student.User.LastName,
                StudentNumber = a.Student.StudentNumber,
                Status = a.Status,
                Notes = a.Notes
            })
            .ToListAsync();

        return attendances;
    }

    public async Task<AttendanceResult> MarkAttendanceAsync(MarkAttendanceDto dto, Guid recordedById)
    {
        try
        {
            var existing = await _context.Attendances
                .FirstOrDefaultAsync(a =>
                    a.StudentId == dto.StudentId &&
                    a.CourseId == dto.CourseId &&
                    a.Date == dto.Date);

            if (existing != null)
            {
                existing.Status = dto.Status;
                existing.Notes = dto.Notes;
            }
            else
            {
                var attendance = new Attendance
                {
                    Id = Guid.NewGuid(),
                    StudentId = dto.StudentId,
                    CourseId = dto.CourseId,
                    Date = dto.Date,
                    Status = dto.Status,
                    Notes = dto.Notes,
                    RecordedById = recordedById,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Attendances.Add(attendance);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Attendance marked for student {StudentId} in course {CourseId} on {Date}",
                dto.StudentId, dto.CourseId, dto.Date);

            return AttendanceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking attendance for student {StudentId}", dto.StudentId);
            return AttendanceResult.Failed("Eroare la marcarea prezenței.");
        }
    }

    public async Task<AttendanceResult> BulkMarkAsync(BulkAttendanceDto dto, Guid recordedById)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            foreach (var attendance in dto.Attendances)
            {
                var existing = await _context.Attendances
                    .FirstOrDefaultAsync(a =>
                        a.StudentId == attendance.StudentId &&
                        a.CourseId == dto.CourseId &&
                        a.Date == dto.Date);

                if (existing != null)
                {
                    existing.Status = attendance.Status;
                    existing.Notes = attendance.Notes;
                }
                else
                {
                    var record = new Attendance
                    {
                        Id = Guid.NewGuid(),
                        StudentId = attendance.StudentId,
                        CourseId = dto.CourseId,
                        Date = dto.Date,
                        Status = attendance.Status,
                        Notes = attendance.Notes,
                        RecordedById = recordedById,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Attendances.Add(record);
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Bulk attendance marked for course {CourseId} on {Date}: {Count} students",
                dto.CourseId, dto.Date, dto.Attendances.Count);

            return AttendanceResult.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error in bulk attendance marking for course {CourseId}", dto.CourseId);
            return AttendanceResult.Failed("Eroare la salvarea prezențelor.");
        }
    }

    public async Task<AttendanceStatsDto> GetStatsAsync(Guid studentId, Guid? courseId)
    {
        var query = _context.Attendances
            .Where(a => a.StudentId == studentId);

        if (courseId.HasValue)
        {
            query = query.Where(a => a.CourseId == courseId.Value);
        }

        var attendances = await query.ToListAsync();

        var totalClasses = attendances.Count;
        var present = attendances.Count(a => a.Status == AttendanceStatus.Present);
        var absent = attendances.Count(a => a.Status == AttendanceStatus.Absent);
        var excused = attendances.Count(a => a.Status == AttendanceStatus.Excused);

        var attendanceRate = totalClasses > 0
            ? (decimal)present / totalClasses * 100
            : 0;

        return new AttendanceStatsDto
        {
            TotalClasses = totalClasses,
            Present = present,
            Absent = absent,
            Excused = excused,
            AttendanceRate = attendanceRate
        };
    }

    public async Task<CalendarMonthDto> GetMonthlyCalendarAsync(Guid studentId, int year, int month)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var attendances = await _context.Attendances
            .Include(a => a.Course)
            .Where(a => a.StudentId == studentId &&
                       a.Date >= startDate &&
                       a.Date <= endDate)
            .ToListAsync();

        var days = new List<CalendarDayDto>();
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            var dayAttendances = attendances
                .Where(a => a.Date == currentDate)
                .Select(a => new AttendanceInfo
                {
                    Course = a.Course.Name,
                    Status = a.Status
                })
                .ToList();

            days.Add(new CalendarDayDto
            {
                Date = currentDate,
                HasClasses = dayAttendances.Any(),
                Attendances = dayAttendances
            });

            currentDate = currentDate.AddDays(1);
        }

        return new CalendarMonthDto
        {
            Year = year,
            Month = month,
            Days = days
        };
    }

    public async Task<List<CourseDto>> GetProfessorCoursesAsync(Guid professorId)
    {
        var courses = await _context.Courses
            .Where(c => c.ProfessorId == professorId && c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CourseDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                Year = c.Year,
                Semester = c.Semester
            })
            .ToListAsync();

        return courses;
    }

    public async Task<List<StudentInCourseDto>> GetStudentsInCourseAsync(Guid courseId)
    {
        var course = await _context.Courses
            .Include(c => c.Program)
                .ThenInclude(p => p.Groups)
                    .ThenInclude(g => g.Students)
                        .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course == null)
        {
            return new List<StudentInCourseDto>();
        }

        var students = course.Program.Groups
            .Where(g => g.Year == course.Year && g.IsActive)
            .SelectMany(g => g.Students)
            .Where(s => s.Status == StudentStatus.Active)
            .OrderBy(s => s.User.LastName)
            .ThenBy(s => s.User.FirstName)
            .Select(s => new StudentInCourseDto
            {
                StudentId = s.Id,
                StudentName = s.User.FirstName + " " + s.User.LastName,
                StudentNumber = s.StudentNumber,
                GroupName = s.Group != null ? s.Group.Name : "Fără grupă"
            })
            .Distinct()
            .ToList();

        return students;
    }
}

// DTOs
public class MarkAttendanceDto
{
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public DateOnly Date { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Notes { get; set; }
}

public class BulkAttendanceDto
{
    public Guid CourseId { get; set; }
    public DateOnly Date { get; set; }
    public List<StudentAttendanceDto> Attendances { get; set; } = new();
}

public class StudentAttendanceDto
{
    public Guid StudentId { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Notes { get; set; }
}

public class AttendanceListDto
{
    public Guid Id { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Notes { get; set; }
}

public class AttendanceRecordDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentNumber { get; set; } = string.Empty;
    public AttendanceStatus Status { get; set; }
    public string? Notes { get; set; }
}

public class AttendanceStatsDto
{
    public int TotalClasses { get; set; }
    public int Present { get; set; }
    public int Absent { get; set; }
    public int Excused { get; set; }
    public decimal AttendanceRate { get; set; }
}

public class CalendarMonthDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public List<CalendarDayDto> Days { get; set; } = new();
}

public class CalendarDayDto
{
    public DateOnly Date { get; set; }
    public bool HasClasses { get; set; }
    public List<AttendanceInfo> Attendances { get; set; } = new();
}

public class AttendanceInfo
{
    public string Course { get; set; } = string.Empty;
    public AttendanceStatus Status { get; set; }
}

public class CourseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Semester { get; set; }
}

public class StudentInCourseDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentNumber { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
}

public class AttendanceResult
{
    public bool Succeeded { get; private set; }
    public string? ErrorMessage { get; private set; }

    public static AttendanceResult Success() => new()
    {
        Succeeded = true
    };

    public static AttendanceResult Failed(string error) => new()
    {
        Succeeded = false,
        ErrorMessage = error
    };
}
