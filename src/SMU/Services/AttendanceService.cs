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
    Task<List<AttendanceListDto>> GetByProfessorCoursesAsync(Guid professorId, Guid? courseId, DateOnly? date);

    // Dean-specific methods
    Task<FacultyAttendanceOverviewDto> GetFacultyOverviewAsync(Guid facultyId, Guid? courseId, int? year);
    Task<List<CourseDto>> GetFacultyCoursesAsync(Guid facultyId);
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
        // Note: Using execution strategy pattern to work with NpgsqlRetryingExecutionStrategy
        // SaveChangesAsync() already uses an implicit transaction, so explicit transaction is not needed
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

            _logger.LogInformation("Bulk attendance marked for course {CourseId} on {Date}: {Count} students",
                dto.CourseId, dto.Date, dto.Attendances.Count);

            return AttendanceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk attendance marking for course {CourseId}. Exception: {Message}. Inner: {Inner}",
                dto.CourseId, ex.Message, ex.InnerException?.Message);
            var errorDetail = ex.InnerException?.Message ?? ex.Message;
            return AttendanceResult.Failed($"Eroare la salvarea prezențelor: {errorDetail}");
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

    public async Task<List<AttendanceListDto>> GetByProfessorCoursesAsync(Guid professorId, Guid? courseId, DateOnly? date)
    {
        // Get professor's courses
        var professorCourseIds = await _context.Courses
            .Where(c => c.ProfessorId == professorId && c.IsActive)
            .Select(c => c.Id)
            .ToListAsync();

        if (!professorCourseIds.Any())
        {
            return new List<AttendanceListDto>();
        }

        var query = _context.Attendances
            .Include(a => a.Course)
            .Include(a => a.Student)
                .ThenInclude(s => s.User)
            .Where(a => professorCourseIds.Contains(a.CourseId));

        // Filter by specific course if provided
        if (courseId.HasValue)
        {
            query = query.Where(a => a.CourseId == courseId.Value);
        }

        // Filter by date if provided
        if (date.HasValue)
        {
            query = query.Where(a => a.Date == date.Value);
        }

        var attendances = await query
            .OrderByDescending(a => a.Date)
            .ThenBy(a => a.Course.Name)
            .Select(a => new AttendanceListDto
            {
                Id = a.Id,
                CourseName = a.Course.Name + " - " + a.Student.User.FirstName + " " + a.Student.User.LastName,
                Date = a.Date,
                Status = a.Status,
                Notes = a.Notes
            })
            .Take(100) // Limit results
            .ToListAsync();

        return attendances;
    }

    public async Task<List<CourseDto>> GetFacultyCoursesAsync(Guid facultyId)
    {
        var courses = await _context.Courses
            .Where(c => c.Program.FacultyId == facultyId && c.IsActive)
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

    public async Task<FacultyAttendanceOverviewDto> GetFacultyOverviewAsync(Guid facultyId, Guid? courseId, int? year)
    {
        // Get all courses in faculty
        var coursesQuery = _context.Courses
            .Where(c => c.Program.FacultyId == facultyId && c.IsActive);

        if (courseId.HasValue)
        {
            coursesQuery = coursesQuery.Where(c => c.Id == courseId.Value);
        }

        if (year.HasValue)
        {
            coursesQuery = coursesQuery.Where(c => c.Year == year.Value);
        }

        var courseIds = await coursesQuery.Select(c => c.Id).ToListAsync();

        // Get all attendance records for these courses
        var attendances = await _context.Attendances
            .Include(a => a.Course)
            .Include(a => a.Student)
                .ThenInclude(s => s.User)
            .Where(a => courseIds.Contains(a.CourseId))
            .ToListAsync();

        // Overall stats
        var totalRecords = attendances.Count;
        var present = attendances.Count(a => a.Status == AttendanceStatus.Present);
        var absent = attendances.Count(a => a.Status == AttendanceStatus.Absent);
        var excused = attendances.Count(a => a.Status == AttendanceStatus.Excused);
        var overallRate = totalRecords > 0 ? (decimal)present / totalRecords * 100 : 0;

        // Stats by course
        var statsByCourse = attendances
            .GroupBy(a => new { a.CourseId, a.Course.Name })
            .Select(g => new CourseAttendanceStatsDto
            {
                CourseId = g.Key.CourseId,
                CourseName = g.Key.Name,
                TotalRecords = g.Count(),
                Present = g.Count(a => a.Status == AttendanceStatus.Present),
                Absent = g.Count(a => a.Status == AttendanceStatus.Absent),
                Excused = g.Count(a => a.Status == AttendanceStatus.Excused),
                AttendanceRate = g.Count() > 0
                    ? (decimal)g.Count(a => a.Status == AttendanceStatus.Present) / g.Count() * 100
                    : 0
            })
            .OrderByDescending(s => s.TotalRecords)
            .ToList();

        // Stats by year (from courses)
        var courseYears = await coursesQuery
            .Select(c => new { c.Id, c.Year })
            .ToListAsync();

        var courseYearDict = courseYears.ToDictionary(c => c.Id, c => c.Year);

        var statsByYear = attendances
            .Where(a => courseYearDict.ContainsKey(a.CourseId))
            .GroupBy(a => courseYearDict[a.CourseId])
            .Select(g => new YearAttendanceStatsDto
            {
                Year = g.Key,
                TotalRecords = g.Count(),
                Present = g.Count(a => a.Status == AttendanceStatus.Present),
                Absent = g.Count(a => a.Status == AttendanceStatus.Absent),
                Excused = g.Count(a => a.Status == AttendanceStatus.Excused),
                AttendanceRate = g.Count() > 0
                    ? (decimal)g.Count(a => a.Status == AttendanceStatus.Present) / g.Count() * 100
                    : 0
            })
            .OrderBy(s => s.Year)
            .ToList();

        // Top students with most absences
        var studentsWithAbsences = attendances
            .Where(a => a.Status == AttendanceStatus.Absent)
            .GroupBy(a => new { a.StudentId, a.Student.User.FirstName, a.Student.User.LastName, a.Student.StudentNumber })
            .Select(g => new StudentAbsenceDto
            {
                StudentId = g.Key.StudentId,
                StudentName = $"{g.Key.FirstName} {g.Key.LastName}",
                StudentNumber = g.Key.StudentNumber,
                AbsenceCount = g.Count()
            })
            .OrderByDescending(s => s.AbsenceCount)
            .Take(10)
            .ToList();

        // Recent attendance records for quick view
        var recentRecords = attendances
            .OrderByDescending(a => a.Date)
            .Take(20)
            .Select(a => new AttendanceListDto
            {
                Id = a.Id,
                CourseName = $"{a.Course.Name} - {a.Student.User.FirstName} {a.Student.User.LastName}",
                Date = a.Date,
                Status = a.Status,
                Notes = a.Notes
            })
            .ToList();

        return new FacultyAttendanceOverviewDto
        {
            TotalRecords = totalRecords,
            Present = present,
            Absent = absent,
            Excused = excused,
            OverallAttendanceRate = overallRate,
            StatsByCourse = statsByCourse,
            StatsByYear = statsByYear,
            TopAbsentStudents = studentsWithAbsences,
            RecentRecords = recentRecords
        };
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

// Dean-specific DTOs
public class FacultyAttendanceOverviewDto
{
    public int TotalRecords { get; set; }
    public int Present { get; set; }
    public int Absent { get; set; }
    public int Excused { get; set; }
    public decimal OverallAttendanceRate { get; set; }
    public List<CourseAttendanceStatsDto> StatsByCourse { get; set; } = new();
    public List<YearAttendanceStatsDto> StatsByYear { get; set; } = new();
    public List<StudentAbsenceDto> TopAbsentStudents { get; set; } = new();
    public List<AttendanceListDto> RecentRecords { get; set; } = new();
}

public class CourseAttendanceStatsDto
{
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int Present { get; set; }
    public int Absent { get; set; }
    public int Excused { get; set; }
    public decimal AttendanceRate { get; set; }
}

public class YearAttendanceStatsDto
{
    public int Year { get; set; }
    public int TotalRecords { get; set; }
    public int Present { get; set; }
    public int Absent { get; set; }
    public int Excused { get; set; }
    public decimal AttendanceRate { get; set; }
}

public class StudentAbsenceDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentNumber { get; set; } = string.Empty;
    public int AbsenceCount { get; set; }
}
