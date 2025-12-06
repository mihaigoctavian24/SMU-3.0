using Microsoft.EntityFrameworkCore;
using SMU.Data;
using SMU.Data.Entities;

namespace SMU.Services.Jobs;

/// <summary>
/// Daily job to create snapshots of academic statistics
/// Runs every day at midnight
/// </summary>
public class DailySnapshotJob : IScheduledJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DailySnapshotJob> _logger;

    public string JobName => "DailySnapshot";
    public TimeSpan Interval => TimeSpan.FromDays(1);

    public DailySnapshotJob(
        IServiceProvider serviceProvider,
        ILogger<DailySnapshotJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting DailySnapshotJob execution");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var snapshotDate = DateTime.UtcNow.Date;

            // Create university-level snapshot
            await CreateUniversitySnapshotAsync(dbContext, snapshotDate, cancellationToken);

            // Create faculty-level snapshots
            await CreateFacultySnapshotsAsync(dbContext, snapshotDate, cancellationToken);

            // Create program-level snapshots
            await CreateProgramSnapshotsAsync(dbContext, snapshotDate, cancellationToken);

            // Create/update grade snapshots for each student
            await CreateGradeSnapshotsAsync(dbContext, snapshotDate, cancellationToken);

            // Create/update attendance stats for each student/course
            await CreateAttendanceStatsAsync(dbContext, snapshotDate, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("DailySnapshotJob completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing DailySnapshotJob");
            throw;
        }
    }

    private async Task CreateUniversitySnapshotAsync(
        ApplicationDbContext dbContext,
        DateTime snapshotDate,
        CancellationToken cancellationToken)
    {
        var stats = await dbContext.Students
            .GroupBy(s => 1)
            .Select(g => new
            {
                TotalStudents = g.Count(),
                ActiveStudents = g.Count(s => s.Status == StudentStatus.Active)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var gradeStats = await dbContext.Grades
            .GroupBy(gr => 1)
            .Select(g => new
            {
                AverageGrade = g.Average(gr => gr.Value),
                GradesSubmitted = g.Count(),
                GradesApproved = g.Count(gr => gr.Status == GradeStatus.Approved)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var attendanceRate = await CalculateAttendanceRateAsync(dbContext, null, null, cancellationToken);

        var snapshot = new DailySnapshot
        {
            Date = DateOnly.FromDateTime(snapshotDate),
            FacultyId = null,
            ProgramId = null,
            TotalStudents = stats?.TotalStudents ?? 0,
            ActiveStudents = stats?.ActiveStudents ?? 0,
            AverageGrade = gradeStats?.AverageGrade ?? 0,
            AttendanceRate = attendanceRate,
            GradesSubmitted = gradeStats?.GradesSubmitted ?? 0,
            GradesApproved = gradeStats?.GradesApproved ?? 0
        };

        dbContext.DailySnapshots.Add(snapshot);
    }

    private async Task CreateFacultySnapshotsAsync(
        ApplicationDbContext dbContext,
        DateTime snapshotDate,
        CancellationToken cancellationToken)
    {
        var faculties = await dbContext.Faculties.ToListAsync(cancellationToken);

        foreach (var faculty in faculties)
        {
            var programIds = await dbContext.Programs
                .Where(p => p.FacultyId == faculty.Id)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            var stats = await dbContext.Students
                .Where(s => s.Group != null && programIds.Contains(s.Group.ProgramId))
                .GroupBy(s => 1)
                .Select(g => new
                {
                    TotalStudents = g.Count(),
                    ActiveStudents = g.Count(s => s.Status == StudentStatus.Active)
                })
                .FirstOrDefaultAsync(cancellationToken);

            var studentIds = await dbContext.Students
                .Where(s => s.Group != null && programIds.Contains(s.Group.ProgramId))
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);

            var gradeStats = await dbContext.Grades
                .Where(gr => studentIds.Contains(gr.StudentId))
                .GroupBy(gr => 1)
                .Select(g => new
                {
                    AverageGrade = g.Average(gr => gr.Value),
                    GradesSubmitted = g.Count(),
                    GradesApproved = g.Count(gr => gr.Status == GradeStatus.Approved)
                })
                .FirstOrDefaultAsync(cancellationToken);

            var attendanceRate = await CalculateAttendanceRateAsync(dbContext, faculty.Id, null, cancellationToken);

            var snapshot = new DailySnapshot
            {
                Date = DateOnly.FromDateTime(snapshotDate),
                FacultyId = faculty.Id,
                ProgramId = null,
                TotalStudents = stats?.TotalStudents ?? 0,
                ActiveStudents = stats?.ActiveStudents ?? 0,
                AverageGrade = gradeStats?.AverageGrade ?? 0,
                AttendanceRate = attendanceRate,
                GradesSubmitted = gradeStats?.GradesSubmitted ?? 0,
                GradesApproved = gradeStats?.GradesApproved ?? 0
            };

            dbContext.DailySnapshots.Add(snapshot);
        }
    }

    private async Task CreateProgramSnapshotsAsync(
        ApplicationDbContext dbContext,
        DateTime snapshotDate,
        CancellationToken cancellationToken)
    {
        var programs = await dbContext.Programs.ToListAsync(cancellationToken);

        foreach (var program in programs)
        {
            var stats = await dbContext.Students
                .Where(s => s.Group != null && s.Group.ProgramId == program.Id)
                .GroupBy(s => 1)
                .Select(g => new
                {
                    TotalStudents = g.Count(),
                    ActiveStudents = g.Count(s => s.Status == StudentStatus.Active)
                })
                .FirstOrDefaultAsync(cancellationToken);

            var studentIds = await dbContext.Students
                .Where(s => s.Group != null && s.Group.ProgramId == program.Id)
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);

            var gradeStats = await dbContext.Grades
                .Where(gr => studentIds.Contains(gr.StudentId))
                .GroupBy(gr => 1)
                .Select(g => new
                {
                    AverageGrade = g.Average(gr => gr.Value),
                    GradesSubmitted = g.Count(),
                    GradesApproved = g.Count(gr => gr.Status == GradeStatus.Approved)
                })
                .FirstOrDefaultAsync(cancellationToken);

            var attendanceRate = await CalculateAttendanceRateAsync(dbContext, program.FacultyId, program.Id, cancellationToken);

            var snapshot = new DailySnapshot
            {
                Date = DateOnly.FromDateTime(snapshotDate),
                FacultyId = program.FacultyId,
                ProgramId = program.Id,
                TotalStudents = stats?.TotalStudents ?? 0,
                ActiveStudents = stats?.ActiveStudents ?? 0,
                AverageGrade = gradeStats?.AverageGrade ?? 0,
                AttendanceRate = attendanceRate,
                GradesSubmitted = gradeStats?.GradesSubmitted ?? 0,
                GradesApproved = gradeStats?.GradesApproved ?? 0
            };

            dbContext.DailySnapshots.Add(snapshot);
        }
    }

    private async Task<decimal> CalculateAttendanceRateAsync(
        ApplicationDbContext dbContext,
        Guid? facultyId,
        Guid? programId,
        CancellationToken cancellationToken)
    {
        IQueryable<Attendance> query = dbContext.Attendances;

        if (facultyId.HasValue && programId.HasValue)
        {
            var studentIds = await dbContext.Students
                .Where(s => s.Group != null && s.Group.ProgramId == programId.Value)
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);

            query = query.Where(a => studentIds.Contains(a.StudentId));
        }
        else if (facultyId.HasValue)
        {
            var programIds = await dbContext.Programs
                .Where(p => p.FacultyId == facultyId.Value)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            var studentIds = await dbContext.Students
                .Where(s => s.Group != null && programIds.Contains(s.Group.ProgramId))
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);

            query = query.Where(a => studentIds.Contains(a.StudentId));
        }

        var attendanceStats = await query
            .GroupBy(a => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Present = g.Count(a => a.Status == AttendanceStatus.Present)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (attendanceStats == null || attendanceStats.Total == 0)
            return 0;

        return (decimal)attendanceStats.Present / attendanceStats.Total * 100;
    }

    private async Task CreateGradeSnapshotsAsync(
        ApplicationDbContext dbContext,
        DateTime snapshotDate,
        CancellationToken cancellationToken)
    {
        var snapshotDateOnly = DateOnly.FromDateTime(snapshotDate);

        // Create grade distribution snapshots per course
        var courses = await dbContext.Courses
            .Include(c => c.Program)
            .Where(c => c.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var course in courses)
        {
            var courseGrades = await dbContext.Grades
                .Where(g => g.CourseId == course.Id && g.Status == GradeStatus.Approved)
                .Select(g => g.Value)
                .ToListAsync(cancellationToken);

            if (!courseGrades.Any())
                continue;

            // Calculate grade distribution
            var grade_1_2 = courseGrades.Count(g => g >= 1 && g < 3);
            var grade_3_4 = courseGrades.Count(g => g >= 3 && g < 5);
            var grade_5_6 = courseGrades.Count(g => g >= 5 && g < 7);
            var grade_7_8 = courseGrades.Count(g => g >= 7 && g < 9);
            var grade_9_10 = courseGrades.Count(g => g >= 9 && g <= 10);
            var avgGrade = courseGrades.Average();
            var passRate = (decimal)courseGrades.Count(g => g >= 5) / courseGrades.Count * 100;

            // Check if snapshot exists for this course/date
            var existingSnapshot = await dbContext.GradeSnapshots
                .FirstOrDefaultAsync(gs => gs.CourseId == course.Id &&
                    gs.SnapshotDate == snapshotDateOnly, cancellationToken);

            if (existingSnapshot != null)
            {
                // Update existing
                existingSnapshot.Grade_1_2 = grade_1_2;
                existingSnapshot.Grade_3_4 = grade_3_4;
                existingSnapshot.Grade_5_6 = grade_5_6;
                existingSnapshot.Grade_7_8 = grade_7_8;
                existingSnapshot.Grade_9_10 = grade_9_10;
                existingSnapshot.AvgGrade = avgGrade;
                existingSnapshot.PassRate = passRate;
            }
            else
            {
                // Create new
                var snapshot = new GradeSnapshot
                {
                    SnapshotDate = snapshotDateOnly,
                    CourseId = course.Id,
                    FacultyId = course.Program.FacultyId,
                    Grade_1_2 = grade_1_2,
                    Grade_3_4 = grade_3_4,
                    Grade_5_6 = grade_5_6,
                    Grade_7_8 = grade_7_8,
                    Grade_9_10 = grade_9_10,
                    AvgGrade = avgGrade,
                    PassRate = passRate
                };

                dbContext.GradeSnapshots.Add(snapshot);
            }
        }

        // Create faculty-level grade distribution snapshots
        var faculties = await dbContext.Faculties.ToListAsync(cancellationToken);

        foreach (var faculty in faculties)
        {
            var facultyCourseIds = await dbContext.Courses
                .Include(c => c.Program)
                .Where(c => c.Program.FacultyId == faculty.Id)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            var facultyGrades = await dbContext.Grades
                .Where(g => facultyCourseIds.Contains(g.CourseId) && g.Status == GradeStatus.Approved)
                .Select(g => g.Value)
                .ToListAsync(cancellationToken);

            if (!facultyGrades.Any())
                continue;

            // Calculate grade distribution for faculty
            var grade_1_2 = facultyGrades.Count(g => g >= 1 && g < 3);
            var grade_3_4 = facultyGrades.Count(g => g >= 3 && g < 5);
            var grade_5_6 = facultyGrades.Count(g => g >= 5 && g < 7);
            var grade_7_8 = facultyGrades.Count(g => g >= 7 && g < 9);
            var grade_9_10 = facultyGrades.Count(g => g >= 9 && g <= 10);
            var avgGrade = facultyGrades.Average();
            var passRate = (decimal)facultyGrades.Count(g => g >= 5) / facultyGrades.Count * 100;

            // Check if snapshot exists for this faculty/date (CourseId = null)
            var existingSnapshot = await dbContext.GradeSnapshots
                .FirstOrDefaultAsync(gs => gs.FacultyId == faculty.Id &&
                    gs.CourseId == null &&
                    gs.SnapshotDate == snapshotDateOnly, cancellationToken);

            if (existingSnapshot != null)
            {
                existingSnapshot.Grade_1_2 = grade_1_2;
                existingSnapshot.Grade_3_4 = grade_3_4;
                existingSnapshot.Grade_5_6 = grade_5_6;
                existingSnapshot.Grade_7_8 = grade_7_8;
                existingSnapshot.Grade_9_10 = grade_9_10;
                existingSnapshot.AvgGrade = avgGrade;
                existingSnapshot.PassRate = passRate;
            }
            else
            {
                var snapshot = new GradeSnapshot
                {
                    SnapshotDate = snapshotDateOnly,
                    CourseId = null,
                    FacultyId = faculty.Id,
                    Grade_1_2 = grade_1_2,
                    Grade_3_4 = grade_3_4,
                    Grade_5_6 = grade_5_6,
                    Grade_7_8 = grade_7_8,
                    Grade_9_10 = grade_9_10,
                    AvgGrade = avgGrade,
                    PassRate = passRate
                };

                dbContext.GradeSnapshots.Add(snapshot);
            }
        }
    }

    private async Task CreateAttendanceStatsAsync(
        ApplicationDbContext dbContext,
        DateTime snapshotDate,
        CancellationToken cancellationToken)
    {
        var activeStudents = await dbContext.Students
            .Where(s => s.Status == StudentStatus.Active)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var studentCourses = await dbContext.Attendances
            .Where(a => activeStudents.Contains(a.StudentId))
            .Select(a => new { a.StudentId, a.CourseId })
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var sc in studentCourses)
        {
            var attendanceData = await dbContext.Attendances
                .Where(a => a.StudentId == sc.StudentId && a.CourseId == sc.CourseId)
                .GroupBy(a => 1)
                .Select(g => new
                {
                    TotalClasses = g.Count(),
                    PresentCount = g.Count(a => a.Status == AttendanceStatus.Present),
                    AbsentCount = g.Count(a => a.Status == AttendanceStatus.Absent),
                    ExcusedCount = g.Count(a => a.Status == AttendanceStatus.Excused)
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (attendanceData == null || attendanceData.TotalClasses == 0)
                continue;

            var attendanceRate = (decimal)attendanceData.PresentCount / attendanceData.TotalClasses * 100;

            // Calculate consecutive absences
            var absences = await dbContext.Attendances
                .Where(a => a.StudentId == sc.StudentId && a.CourseId == sc.CourseId && a.Status == AttendanceStatus.Absent)
                .OrderBy(a => a.Date)
                .Select(a => a.Date)
                .ToListAsync(cancellationToken);

            int consecutiveAbsences = CalculateConsecutiveAbsences(absences);

            // Check if stats exist
            var existingStats = await dbContext.AttendanceStats
                .FirstOrDefaultAsync(
                    ast => ast.StudentId == sc.StudentId && ast.CourseId == sc.CourseId,
                    cancellationToken);

            if (existingStats != null)
            {
                // Update existing
                existingStats.TotalClasses = attendanceData.TotalClasses;
                existingStats.PresentCount = attendanceData.PresentCount;
                existingStats.AbsentCount = attendanceData.AbsentCount;
                existingStats.ExcusedCount = attendanceData.ExcusedCount;
                existingStats.AttendanceRate = attendanceRate;
                existingStats.ConsecutiveAbsences = consecutiveAbsences;
                existingStats.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                // Create new
                var stats = new AttendanceStats
                {
                    StudentId = sc.StudentId,
                    CourseId = sc.CourseId,
                    TotalClasses = attendanceData.TotalClasses,
                    PresentCount = attendanceData.PresentCount,
                    AbsentCount = attendanceData.AbsentCount,
                    ExcusedCount = attendanceData.ExcusedCount,
                    AttendanceRate = attendanceRate,
                    ConsecutiveAbsences = consecutiveAbsences,
                    LastUpdated = DateTime.UtcNow
                };

                dbContext.AttendanceStats.Add(stats);
            }
        }
    }

    private int CalculateConsecutiveAbsences(List<DateOnly> absenceDates)
    {
        if (!absenceDates.Any())
            return 0;

        int maxConsecutive = 1;
        int currentConsecutive = 1;

        for (int i = 1; i < absenceDates.Count; i++)
        {
            var daysDiff = absenceDates[i].DayNumber - absenceDates[i - 1].DayNumber;
            if (daysDiff <= 7) // Consider weekly schedule
            {
                currentConsecutive++;
                maxConsecutive = Math.Max(maxConsecutive, currentConsecutive);
            }
            else
            {
                currentConsecutive = 1;
            }
        }

        return maxConsecutive;
    }
}
