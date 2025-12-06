using Microsoft.EntityFrameworkCore;
using SMU.Data;
using SMU.Data.Entities;

namespace SMU.Services;

/// <summary>
/// Analytics service providing comprehensive statistics for all user roles
/// </summary>
public interface IAnalyticsService
{
    // Student Analytics
    Task<StudentAnalyticsDto> GetStudentStatsAsync(Guid studentId);
    Task<List<GradeEvolutionPoint>> GetGradeEvolutionAsync(Guid studentId, int months = 12);
    Task<AnalyticsAttendanceStatsDto> GetAttendanceStatsAsync(Guid studentId);

    // Professor Analytics
    Task<ProfessorAnalyticsDto> GetProfessorStatsAsync(Guid professorId);
    Task<List<CoursePerformanceDto>> GetCoursePerformancesAsync(Guid professorId);
    Task<GradeDistributionDto> GetProfessorGradeDistributionAsync(Guid professorId);

    // Faculty Analytics (Dean)
    Task<FacultyAnalyticsDto> GetFacultyStatsAsync(Guid facultyId);
    Task<List<ProgramPerformanceDto>> GetProgramPerformancesAsync(Guid facultyId);
    Task<GradeDistributionDto> GetFacultyGradeDistributionAsync(Guid facultyId);

    // University Analytics (Rector)
    Task<UniversityAnalyticsDto> GetUniversityStatsAsync();
    Task<List<FacultyComparisonDto>> GetFacultyComparisonsAsync();
    Task<TrendDataDto> GetUniversityTrendsAsync(int months = 6);
}

public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(
        ApplicationDbContext context,
        ILogger<AnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Student Analytics

    public async Task<StudentAnalyticsDto> GetStudentStatsAsync(Guid studentId)
    {
        var student = await _context.Students
            .Include(s => s.Grades.Where(g => g.Status == GradeStatus.Approved))
                .ThenInclude(g => g.Course)
            .Include(s => s.Attendances)
                .ThenInclude(a => a.Course)
            .FirstOrDefaultAsync(s => s.Id == studentId);

        if (student == null)
        {
            _logger.LogWarning("Student not found: {StudentId}", studentId);
            return new StudentAnalyticsDto();
        }

        var approvedGrades = student.Grades.Where(g => g.Status == GradeStatus.Approved).ToList();
        var averageGrade = approvedGrades.Any()
            ? approvedGrades.Average(g => g.Value)
            : 0;

        var totalCredits = approvedGrades
            .Where(g => g.Value >= 5) // Passing grade in Romanian system
            .Sum(g => g.Course.Credits);

        var totalAttendances = student.Attendances.Count;
        var presentCount = student.Attendances.Count(a => a.Status == AttendanceStatus.Present);
        var attendanceRate = totalAttendances > 0
            ? (decimal)presentCount / totalAttendances * 100
            : 0;

        var courseCount = approvedGrades.Select(g => g.CourseId).Distinct().Count();

        // Calculate risk level
        var riskLevel = CalculateStudentRiskLevel(averageGrade, attendanceRate);

        return new StudentAnalyticsDto
        {
            AverageGrade = averageGrade,
            TotalCredits = totalCredits,
            AttendanceRate = attendanceRate,
            CourseCount = courseCount,
            RiskLevel = riskLevel,
            PassingGradeCount = approvedGrades.Count(g => g.Value >= 5),
            FailingGradeCount = approvedGrades.Count(g => g.Value < 5)
        };
    }

    public async Task<List<GradeEvolutionPoint>> GetGradeEvolutionAsync(Guid studentId, int months = 12)
    {
        var cutoffDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-months));

        var grades = await _context.Grades
            .Include(g => g.Course)
            .Where(g => g.StudentId == studentId
                && g.Status == GradeStatus.Approved
                && g.ExamDate >= cutoffDate)
            .OrderBy(g => g.ExamDate)
            .ToListAsync();

        if (!grades.Any())
        {
            return new List<GradeEvolutionPoint>();
        }

        // Group by month and calculate average
        var evolutionPoints = grades
            .GroupBy(g => new { g.ExamDate.Year, g.ExamDate.Month })
            .Select(g => new GradeEvolutionPoint
            {
                Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                Average = g.Average(x => x.Value),
                CourseName = $"{g.Count()} cursuri"
            })
            .OrderBy(p => p.Date)
            .ToList();

        return evolutionPoints;
    }

    public async Task<AnalyticsAttendanceStatsDto> GetAttendanceStatsAsync(Guid studentId)
    {
        var attendances = await _context.Attendances
            .Include(a => a.Course)
            .Where(a => a.StudentId == studentId)
            .ToListAsync();

        if (!attendances.Any())
        {
            return new AnalyticsAttendanceStatsDto();
        }

        var presentCount = attendances.Count(a => a.Status == AttendanceStatus.Present);
        var absentCount = attendances.Count(a => a.Status == AttendanceStatus.Absent);
        var excusedCount = attendances.Count(a => a.Status == AttendanceStatus.Excused);
        var total = attendances.Count;

        var overallRate = total > 0 ? (decimal)presentCount / total * 100 : 0;

        // Group by course type (extract from course type or use default)
        var byCourseType = attendances
            .GroupBy(a => a.Course.Name.Contains("Laborator") ? "Laborator" : "Curs")
            .ToDictionary(
                g => g.Key,
                g => new AnalyticsAttendanceRateDto
                {
                    Total = g.Count(),
                    Present = g.Count(a => a.Status == AttendanceStatus.Present),
                    Rate = g.Count() > 0 ? (decimal)g.Count(a => a.Status == AttendanceStatus.Present) / g.Count() * 100 : 0
                });

        return new AnalyticsAttendanceStatsDto
        {
            PresentCount = presentCount,
            AbsentCount = absentCount,
            ExcusedCount = excusedCount,
            OverallRate = overallRate,
            RateByCourseType = byCourseType
        };
    }

    #endregion

    #region Professor Analytics

    public async Task<ProfessorAnalyticsDto> GetProfessorStatsAsync(Guid professorId)
    {
        var professor = await _context.Professors
            .Include(p => p.Courses)
                .ThenInclude(c => c.Grades.Where(g => g.Status == GradeStatus.Approved))
            .FirstOrDefaultAsync(p => p.Id == professorId);

        if (professor == null)
        {
            _logger.LogWarning("Professor not found: {ProfessorId}", professorId);
            return new ProfessorAnalyticsDto();
        }

        var courseCount = professor.Courses.Count(c => c.IsActive);

        var allGrades = professor.Courses
            .SelectMany(c => c.Grades)
            .Where(g => g.Status == GradeStatus.Approved)
            .ToList();

        var totalStudents = allGrades.Select(g => g.StudentId).Distinct().Count();
        var averageGradeGiven = allGrades.Any() ? allGrades.Average(g => g.Value) : 0;

        var pendingGrades = await _context.Grades
            .Where(g => g.EnteredById == professorId && g.Status == GradeStatus.Pending)
            .CountAsync();

        return new ProfessorAnalyticsDto
        {
            CourseCount = courseCount,
            TotalStudents = totalStudents,
            AverageGradeGiven = averageGradeGiven,
            PendingGrades = pendingGrades
        };
    }

    public async Task<List<CoursePerformanceDto>> GetCoursePerformancesAsync(Guid professorId)
    {
        var courses = await _context.Courses
            .Include(c => c.Grades.Where(g => g.Status == GradeStatus.Approved))
            .Include(c => c.Attendances)
            .Where(c => c.ProfessorId == professorId && c.IsActive)
            .ToListAsync();

        var performances = courses.Select(c =>
        {
            var approvedGrades = c.Grades.Where(g => g.Status == GradeStatus.Approved).ToList();
            var studentCount = approvedGrades.Select(g => g.StudentId).Distinct().Count();
            var averageGrade = approvedGrades.Any() ? approvedGrades.Average(g => g.Value) : 0;
            var passRate = approvedGrades.Any()
                ? (decimal)approvedGrades.Count(g => g.Value >= 5) / approvedGrades.Count() * 100
                : 0;

            var attendances = c.Attendances.ToList();
            var attendanceRate = attendances.Any()
                ? (decimal)attendances.Count(a => a.Status == AttendanceStatus.Present) / attendances.Count() * 100
                : 0;

            return new CoursePerformanceDto
            {
                CourseName = c.Name,
                CourseCode = c.Code,
                StudentCount = studentCount,
                AverageGrade = averageGrade,
                PassRate = passRate,
                AttendanceRate = attendanceRate
            };
        }).OrderByDescending(p => p.StudentCount).ToList();

        return performances;
    }

    public async Task<GradeDistributionDto> GetProfessorGradeDistributionAsync(Guid professorId)
    {
        var courses = await _context.Courses
            .Include(c => c.Grades.Where(g => g.Status == GradeStatus.Approved))
            .Where(c => c.ProfessorId == professorId && c.IsActive)
            .ToListAsync();

        var allGrades = courses
            .SelectMany(c => c.Grades)
            .Where(g => g.Status == GradeStatus.Approved)
            .ToList();

        var distribution = new Dictionary<int, int>();
        for (int i = 1; i <= 10; i++)
        {
            distribution[i] = allGrades.Count(g => (int)Math.Round(g.Value) == i);
        }

        return new GradeDistributionDto
        {
            Distribution = distribution,
            TotalGrades = allGrades.Count
        };
    }

    #endregion

    #region Faculty Analytics (Dean)

    public async Task<FacultyAnalyticsDto> GetFacultyStatsAsync(Guid facultyId)
    {
        var faculty = await _context.Faculties
            .Include(f => f.Programs)
                .ThenInclude(p => p.Groups)
                    .ThenInclude(g => g.Students)
                        .ThenInclude(s => s.Grades.Where(gr => gr.Status == GradeStatus.Approved))
            .Include(f => f.Professors)
            .FirstOrDefaultAsync(f => f.Id == facultyId);

        if (faculty == null)
        {
            _logger.LogWarning("Faculty not found: {FacultyId}", facultyId);
            return new FacultyAnalyticsDto();
        }

        var studentCount = faculty.Programs
            .SelectMany(p => p.Groups)
            .SelectMany(g => g.Students)
            .Where(s => s.Status == StudentStatus.Active)
            .Count();

        var professorCount = faculty.Professors.Count();
        var programCount = faculty.Programs.Count(p => p.IsActive);

        var allGrades = faculty.Programs
            .SelectMany(p => p.Groups)
            .SelectMany(g => g.Students)
            .SelectMany(s => s.Grades)
            .Where(g => g.Status == GradeStatus.Approved)
            .ToList();

        var averageGrade = allGrades.Any() ? allGrades.Average(g => g.Value) : 0;
        var passRate = allGrades.Any()
            ? (decimal)allGrades.Count(g => g.Value >= 5) / allGrades.Count() * 100
            : 0;

        return new FacultyAnalyticsDto
        {
            FacultyName = faculty.Name,
            StudentCount = studentCount,
            ProfessorCount = professorCount,
            ProgramCount = programCount,
            AverageGrade = averageGrade,
            PassRate = passRate
        };
    }

    public async Task<List<ProgramPerformanceDto>> GetProgramPerformancesAsync(Guid facultyId)
    {
        var programs = await _context.Programs
            .Include(p => p.Groups)
                .ThenInclude(g => g.Students)
                    .ThenInclude(s => s.Grades.Where(gr => gr.Status == GradeStatus.Approved))
            .Include(p => p.Groups)
                .ThenInclude(g => g.Students)
                    .ThenInclude(s => s.Attendances)
            .Where(p => p.FacultyId == facultyId && p.IsActive)
            .ToListAsync();

        var performances = programs.Select(p =>
        {
            var students = p.Groups.SelectMany(g => g.Students).ToList();
            var studentCount = students.Count(s => s.Status == StudentStatus.Active);

            var allGrades = students
                .SelectMany(s => s.Grades)
                .Where(g => g.Status == GradeStatus.Approved)
                .ToList();

            var averageGrade = allGrades.Any() ? allGrades.Average(g => g.Value) : 0;

            var allAttendances = students.SelectMany(s => s.Attendances).ToList();
            var attendanceRate = allAttendances.Any()
                ? (decimal)allAttendances.Count(a => a.Status == AttendanceStatus.Present) / allAttendances.Count() * 100
                : 0;

            return new ProgramPerformanceDto
            {
                ProgramName = p.Name,
                ProgramCode = p.Code,
                StudentCount = studentCount,
                AverageGrade = averageGrade,
                AttendanceRate = attendanceRate
            };
        }).OrderByDescending(p => p.StudentCount).ToList();

        return performances;
    }

    public async Task<GradeDistributionDto> GetFacultyGradeDistributionAsync(Guid facultyId)
    {
        var grades = await _context.Grades
            .Include(g => g.Student)
                .ThenInclude(s => s.Group)
                    .ThenInclude(gr => gr.Program)
            .Where(g => g.Status == GradeStatus.Approved
                && g.Student.Group != null
                && g.Student.Group.Program.FacultyId == facultyId)
            .ToListAsync();

        var distribution = new Dictionary<int, int>();
        for (int i = 1; i <= 10; i++)
        {
            distribution[i] = 0;
        }

        foreach (var grade in grades)
        {
            var gradeValue = (int)Math.Floor(grade.Value);
            if (gradeValue >= 1 && gradeValue <= 10)
            {
                distribution[gradeValue]++;
            }
        }

        return new GradeDistributionDto
        {
            Distribution = distribution,
            TotalGrades = grades.Count
        };
    }

    #endregion

    #region University Analytics (Rector)

    public async Task<UniversityAnalyticsDto> GetUniversityStatsAsync()
    {
        var totalStudents = await _context.Students
            .CountAsync(s => s.Status == StudentStatus.Active);

        var totalProfessors = await _context.Professors.CountAsync();
        var facultyCount = await _context.Faculties.CountAsync(f => f.IsActive);
        var programCount = await _context.Programs.CountAsync(p => p.IsActive);
        var courseCount = await _context.Courses.CountAsync(c => c.IsActive);
        var groupCount = await _context.Groups.CountAsync(g => g.IsActive);

        var allGrades = await _context.Grades
            .Where(g => g.Status == GradeStatus.Approved)
            .ToListAsync();

        var overallAverage = allGrades.Any() ? allGrades.Average(g => g.Value) : 0;
        var passRate = allGrades.Any()
            ? (decimal)allGrades.Count(g => g.Value >= 5) / allGrades.Count() * 100
            : 0;

        // Calculate at-risk students (average below 5 or low attendance)
        var atRiskCount = await _context.Students
            .Where(s => s.Status == StudentStatus.Active)
            .Where(s => s.Grades.Where(g => g.Status == GradeStatus.Approved).Any() &&
                        s.Grades.Where(g => g.Status == GradeStatus.Approved).Average(g => g.Value) < 5)
            .CountAsync();

        return new UniversityAnalyticsDto
        {
            TotalStudents = totalStudents,
            TotalProfessors = totalProfessors,
            FacultyCount = facultyCount,
            ProgramCount = programCount,
            CourseCount = courseCount,
            GroupCount = groupCount,
            OverallAverage = overallAverage,
            PassRate = passRate,
            AtRiskStudentsCount = atRiskCount
        };
    }

    public async Task<List<FacultyComparisonDto>> GetFacultyComparisonsAsync()
    {
        var faculties = await _context.Faculties
            .Include(f => f.Programs)
                .ThenInclude(p => p.Groups)
                    .ThenInclude(g => g.Students)
                        .ThenInclude(s => s.Grades.Where(gr => gr.Status == GradeStatus.Approved))
            .Where(f => f.IsActive)
            .ToListAsync();

        // Get professors separately since they're not included in Faculties directly
        var professors = await _context.Professors.ToListAsync();

        var comparisons = faculties.Select(f =>
        {
            var students = f.Programs
                .SelectMany(p => p.Groups)
                .SelectMany(g => g.Students)
                .ToList();

            var studentCount = students.Count(s => s.Status == StudentStatus.Active);
            var professorCount = professors.Count(p => p.FacultyId == f.Id);
            var programCount = f.Programs.Count;

            var grades = students
                .SelectMany(s => s.Grades)
                .Where(g => g.Status == GradeStatus.Approved)
                .ToList();

            var averageGrade = grades.Any() ? grades.Average(g => g.Value) : 0;
            var passRate = grades.Any()
                ? (decimal)grades.Count(g => g.Value >= 5) / grades.Count() * 100
                : 0;

            // Simple trend calculation: compare last 3 months vs previous 3 months
            var threeMonthsAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-3));
            var sixMonthsAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6));

            var recentAvg = grades
                .Where(g => g.ExamDate >= threeMonthsAgo)
                .Select(g => g.Value)
                .DefaultIfEmpty(0)
                .Average();

            var previousAvg = grades
                .Where(g => g.ExamDate >= sixMonthsAgo && g.ExamDate < threeMonthsAgo)
                .Select(g => g.Value)
                .DefaultIfEmpty(0)
                .Average();

            var trend = recentAvg > previousAvg ? "ascending" :
                        recentAvg < previousAvg ? "descending" : "stable";

            // Calculate trend value as percentage difference
            var trendValue = previousAvg > 0
                ? Math.Round((recentAvg - previousAvg) / previousAvg * 100, 1)
                : 0;

            return new FacultyComparisonDto
            {
                FacultyId = f.Id,
                FacultyName = f.Name,
                FacultyCode = f.Code,
                StudentCount = studentCount,
                ProfessorCount = professorCount,
                ProgramCount = programCount,
                AverageGrade = averageGrade,
                PassRate = passRate,
                Trend = trend,
                TrendValue = (decimal)trendValue
            };
        }).OrderByDescending(c => c.StudentCount).ToList();

        return comparisons;
    }

    public async Task<TrendDataDto> GetUniversityTrendsAsync(int months = 6)
    {
        var cutoffDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-months));

        var grades = await _context.Grades
            .Where(g => g.Status == GradeStatus.Approved && g.ExamDate >= cutoffDate)
            .ToListAsync();

        var monthlyData = grades
            .GroupBy(g => new { g.ExamDate.Year, g.ExamDate.Month })
            .Select(g => new MonthlyTrendPoint
            {
                Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                AverageGrade = g.Average(x => x.Value),
                PassRate = (decimal)g.Count(x => x.Value >= 5) / g.Count() * 100,
                TotalGrades = g.Count()
            })
            .OrderBy(p => p.Date)
            .ToList();

        return new TrendDataDto
        {
            MonthlyData = monthlyData
        };
    }

    #endregion

    #region Helper Methods

    private RiskLevel CalculateStudentRiskLevel(decimal averageGrade, decimal attendanceRate)
    {
        // Critical: Failing average or very low attendance
        if (averageGrade < 5 || attendanceRate < 50)
            return RiskLevel.Critical;

        // High: Low grades or poor attendance
        if (averageGrade < 6 || attendanceRate < 65)
            return RiskLevel.High;

        // Medium: Below average performance
        if (averageGrade < 7 || attendanceRate < 75)
            return RiskLevel.Medium;

        // Low: Good performance
        return RiskLevel.Low;
    }

    #endregion
}

#region DTOs

/// <summary>
/// Student analytics summary
/// </summary>
public class StudentAnalyticsDto
{
    public decimal AverageGrade { get; set; }
    public int TotalCredits { get; set; }
    public decimal AttendanceRate { get; set; }
    public int CourseCount { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public int PassingGradeCount { get; set; }
    public int FailingGradeCount { get; set; }
}

/// <summary>
/// Grade evolution point for line chart
/// </summary>
public class GradeEvolutionPoint
{
    public DateTime Date { get; set; }
    public decimal Average { get; set; }
    public string CourseName { get; set; } = string.Empty;
}

/// <summary>
/// Attendance statistics for analytics (different from AttendanceService.AttendanceStatsDto)
/// </summary>
public class AnalyticsAttendanceStatsDto
{
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int ExcusedCount { get; set; }
    public decimal OverallRate { get; set; }
    public Dictionary<string, AnalyticsAttendanceRateDto> RateByCourseType { get; set; } = new();
}

public class AnalyticsAttendanceRateDto
{
    public int Total { get; set; }
    public int Present { get; set; }
    public decimal Rate { get; set; }
}

/// <summary>
/// Professor analytics summary
/// </summary>
public class ProfessorAnalyticsDto
{
    public int CourseCount { get; set; }
    public int TotalStudents { get; set; }
    public decimal AverageGradeGiven { get; set; }
    public int PendingGrades { get; set; }
}

/// <summary>
/// Course performance metrics for a professor
/// </summary>
public class CoursePerformanceDto
{
    public string CourseName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public decimal AverageGrade { get; set; }
    public decimal PassRate { get; set; }
    public decimal AttendanceRate { get; set; }
}

/// <summary>
/// Faculty analytics summary for Dean
/// </summary>
public class FacultyAnalyticsDto
{
    public string FacultyName { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public int ProfessorCount { get; set; }
    public int ProgramCount { get; set; }
    public decimal AverageGrade { get; set; }
    public decimal PassRate { get; set; }
}

/// <summary>
/// Program performance metrics within a faculty
/// </summary>
public class ProgramPerformanceDto
{
    public string ProgramName { get; set; } = string.Empty;
    public string ProgramCode { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public decimal AverageGrade { get; set; }
    public decimal AttendanceRate { get; set; }
}

/// <summary>
/// Grade distribution for faculty (1-10 scale)
/// </summary>
public class GradeDistributionDto
{
    public Dictionary<int, int> Distribution { get; set; } = new();
    public int TotalGrades { get; set; }
}

/// <summary>
/// University-wide analytics for Rector
/// </summary>
public class UniversityAnalyticsDto
{
    public int TotalStudents { get; set; }
    public int TotalProfessors { get; set; }
    public int FacultyCount { get; set; }
    public int ProgramCount { get; set; }
    public int CourseCount { get; set; }
    public int GroupCount { get; set; }
    public decimal OverallAverage { get; set; }
    public decimal PassRate { get; set; }
    public int AtRiskStudentsCount { get; set; }
}

/// <summary>
/// Faculty comparison data for Rector dashboard
/// </summary>
public class FacultyComparisonDto
{
    public Guid FacultyId { get; set; }
    public string FacultyName { get; set; } = string.Empty;
    public string FacultyCode { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public int ProfessorCount { get; set; }
    public int ProgramCount { get; set; }
    public decimal AverageGrade { get; set; }
    public decimal PassRate { get; set; }
    public string Trend { get; set; } = "stable"; // "ascending", "descending", "stable"
    public decimal TrendValue { get; set; } // Percentage change
}

/// <summary>
/// University trend data over time
/// </summary>
public class TrendDataDto
{
    public List<MonthlyTrendPoint> MonthlyData { get; set; } = new();
}

public class MonthlyTrendPoint
{
    public DateTime Date { get; set; }
    public decimal AverageGrade { get; set; }
    public decimal PassRate { get; set; }
    public int TotalGrades { get; set; }
}

#endregion
