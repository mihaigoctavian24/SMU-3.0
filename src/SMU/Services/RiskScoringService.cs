using Microsoft.EntityFrameworkCore;
using SMU.Data;
using SMU.Data.Entities;
using System.Text.Json;

namespace SMU.Services;

/// <summary>
/// Implementation of predictive analytics risk scoring service
/// Calculates student risk scores based on multiple weighted factors
/// </summary>
public class RiskScoringService : IRiskScoringService
{
    private readonly ApplicationDbContext _context;

    // Risk Factor Weights (from PRD)
    private const decimal WEIGHT_CURRENT_GRADE = 0.25m;        // 25%
    private const decimal WEIGHT_GRADE_TREND = 0.20m;          // 20%
    private const decimal WEIGHT_ATTENDANCE_RATE = 0.20m;      // 20%
    private const decimal WEIGHT_CONSECUTIVE_ABSENCES = 0.15m; // 15%
    private const decimal WEIGHT_FAILED_COURSES = 0.10m;       // 10%
    private const decimal WEIGHT_ENGAGEMENT = 0.10m;           // 10%

    // Risk Level Thresholds
    private const int THRESHOLD_LOW = 30;
    private const int THRESHOLD_MEDIUM = 60;
    private const int THRESHOLD_HIGH = 80;

    public RiskScoringService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StudentRiskScore> CalculateStudentRiskAsync(Guid studentId)
    {
        var student = await _context.Students
            .Include(s => s.Grades)
            .Include(s => s.Attendances)
            .Include(s => s.Group)
            .FirstOrDefaultAsync(s => s.Id == studentId);

        if (student == null)
            throw new InvalidOperationException($"Student {studentId} not found");

        // Calculate individual risk factors
        var riskFactors = await GetRiskFactorsAsync(studentId);

        // Calculate weighted overall score (0-100)
        var overallScore = (int)Math.Round(
            (riskFactors.CurrentGradeScore * WEIGHT_CURRENT_GRADE) +
            (riskFactors.GradeTrendScore * WEIGHT_GRADE_TREND) +
            (riskFactors.AttendanceScore * WEIGHT_ATTENDANCE_RATE) +
            (riskFactors.ConsecutiveAbsencesScore * WEIGHT_CONSECUTIVE_ABSENCES) +
            (riskFactors.FailedCoursesScore * WEIGHT_FAILED_COURSES) +
            (riskFactors.EngagementScore * WEIGHT_ENGAGEMENT)
        );

        // Determine risk level based on score
        var riskLevel = DetermineRiskLevel(overallScore);

        // Get recommendations
        var recommendations = await GetRecommendationsAsync(riskLevel);

        // Create or update risk score
        var existingScore = await _context.Set<StudentRiskScore>()
            .FirstOrDefaultAsync(r => r.StudentId == studentId);

        if (existingScore != null)
        {
            existingScore.OverallScore = overallScore;
            existingScore.Level = riskLevel;
            existingScore.GradeRiskFactor = riskFactors.CurrentGradeScore;
            existingScore.AttendanceRiskFactor = riskFactors.AttendanceScore;
            existingScore.TrendRiskFactor = riskFactors.GradeTrendScore;
            existingScore.EngagementRiskFactor = riskFactors.EngagementScore;
            existingScore.RiskFactors = JsonSerializer.Serialize(GetRiskFactorsList(riskFactors));
            existingScore.Recommendations = JsonSerializer.Serialize(recommendations);
            existingScore.CalculatedAt = DateTime.UtcNow;
        }
        else
        {
            existingScore = new StudentRiskScore
            {
                StudentId = studentId,
                OverallScore = overallScore,
                Level = riskLevel,
                GradeRiskFactor = riskFactors.CurrentGradeScore,
                AttendanceRiskFactor = riskFactors.AttendanceScore,
                TrendRiskFactor = riskFactors.GradeTrendScore,
                EngagementRiskFactor = riskFactors.EngagementScore,
                RiskFactors = JsonSerializer.Serialize(GetRiskFactorsList(riskFactors)),
                Recommendations = JsonSerializer.Serialize(recommendations),
                CalculatedAt = DateTime.UtcNow
            };
            _context.Set<StudentRiskScore>().Add(existingScore);
        }

        await _context.SaveChangesAsync();
        return existingScore;
    }

    public async Task<RiskFactorsDto> GetRiskFactorsAsync(Guid studentId)
    {
        var student = await _context.Students
            .Include(s => s.Grades.Where(g => g.Status == GradeStatus.Approved))
            .Include(s => s.Attendances)
            .FirstOrDefaultAsync(s => s.Id == studentId);

        if (student == null)
            throw new InvalidOperationException($"Student {studentId} not found");

        var dto = new RiskFactorsDto
        {
            StudentId = studentId
        };

        // 1. Current Grade Risk (0-100, higher = worse)
        var approvedGrades = student.Grades
            .Where(g => g.Status == GradeStatus.Approved)
            .ToList();

        if (approvedGrades.Any())
        {
            var avgGrade = approvedGrades.Average(g => g.Value);
            dto.Details["AverageGrade"] = avgGrade;

            // Risk increases as grade decreases below 7.0
            // 10.0 → 0 risk, 5.0 → 100 risk
            dto.CurrentGradeScore = avgGrade >= 7.0m
                ? 0m
                : Math.Min(100m, (7.0m - avgGrade) / 2.0m * 100m);
        }
        else
        {
            dto.CurrentGradeScore = 50m; // No data = medium risk
            dto.Details["AverageGrade"] = "No grades";
        }

        // 2. Grade Trend Risk (0-100)
        if (approvedGrades.Count >= 3)
        {
            var recentGrades = approvedGrades
                .OrderByDescending(g => g.ExamDate)
                .Take(3)
                .ToList();

            var olderGrades = approvedGrades
                .OrderByDescending(g => g.ExamDate)
                .Skip(3)
                .Take(3)
                .ToList();

            if (olderGrades.Any())
            {
                var recentAvg = recentGrades.Average(g => g.Value);
                var olderAvg = olderGrades.Average(g => g.Value);
                var trend = olderAvg - recentAvg; // Positive = declining

                dto.Details["GradeTrend"] = trend > 0 ? "Declining" : "Improving";
                dto.Details["TrendValue"] = trend;

                // Risk increases with declining trend
                dto.GradeTrendScore = trend > 0
                    ? Math.Min(100m, trend * 30m)
                    : 0m;
            }
            else
            {
                dto.GradeTrendScore = 0m;
                dto.Details["GradeTrend"] = "Insufficient data";
            }
        }
        else
        {
            dto.GradeTrendScore = 0m;
            dto.Details["GradeTrend"] = "Too few grades";
        }

        // 3. Attendance Rate Risk (0-100)
        var totalAttendances = student.Attendances.Count;
        if (totalAttendances > 0)
        {
            var presentCount = student.Attendances.Count(a => a.Status == AttendanceStatus.Present);
            var attendanceRate = (decimal)presentCount / totalAttendances;
            dto.Details["AttendanceRate"] = $"{attendanceRate:P1}";

            // Risk increases as attendance drops below 80%
            dto.AttendanceScore = attendanceRate >= 0.80m
                ? 0m
                : Math.Min(100m, (0.80m - attendanceRate) * 250m);
        }
        else
        {
            dto.AttendanceScore = 0m;
            dto.Details["AttendanceRate"] = "No records";
        }

        // 4. Consecutive Absences Risk (0-100)
        var consecutiveAbsences = CalculateMaxConsecutiveAbsences(student.Attendances.ToList());
        dto.Details["ConsecutiveAbsences"] = consecutiveAbsences;
        dto.ConsecutiveAbsencesScore = consecutiveAbsences >= 5
            ? 100m
            : Math.Min(100m, consecutiveAbsences * 25m);

        // 5. Failed Courses History Risk (0-100)
        var failedCount = approvedGrades.Count(g => g.Value < 5.0m);
        dto.Details["FailedCourses"] = failedCount;
        dto.FailedCoursesScore = failedCount == 0
            ? 0m
            : Math.Min(100m, failedCount * 30m);

        // 6. Platform Engagement Risk (0-100)
        // For now, use a simple heuristic based on data presence
        var hasRecentActivity = student.Grades.Any(g => g.CreatedAt > DateTime.UtcNow.AddDays(-30))
            || student.Attendances.Any(a => a.CreatedAt > DateTime.UtcNow.AddDays(-30));

        dto.EngagementScore = hasRecentActivity ? 0m : 50m;
        dto.Details["RecentActivity"] = hasRecentActivity;

        return dto;
    }

    public async Task<List<string>> GetRecommendationsAsync(RiskLevel riskLevel)
    {
        return await Task.FromResult(riskLevel switch
        {
            RiskLevel.Low => new List<string>
            {
                "Continue current study habits",
                "Maintain regular attendance",
                "No immediate intervention required"
            },
            RiskLevel.Medium => new List<string>
            {
                "Monitor academic progress closely",
                "Notify tutor for follow-up",
                "Recommend study group participation",
                "Schedule optional consultation sessions"
            },
            RiskLevel.High => new List<string>
            {
                "Immediate tutor intervention required",
                "Schedule mandatory counseling session",
                "Develop personalized improvement plan",
                "Increase monitoring frequency",
                "Notify parents/guardians if applicable"
            },
            RiskLevel.Critical => new List<string>
            {
                "URGENT: Immediate faculty intervention",
                "Emergency academic counseling",
                "Develop intensive recovery program",
                "Daily progress monitoring",
                "Consider academic probation review",
                "Involve dean and academic committee"
            },
            _ => new List<string> { "Unknown risk level" }
        });
    }

    public async Task<int> BulkCalculateRisksAsync(Guid? facultyId = null)
    {
        var studentsQuery = _context.Students.AsQueryable();

        if (facultyId.HasValue)
        {
            studentsQuery = studentsQuery
                .Include(s => s.Group)
                    .ThenInclude(g => g.Program)
                    .ThenInclude(p => p.Faculty)
                .Where(s => s.Group != null && s.Group.Program.FacultyId == facultyId.Value);
        }

        var students = await studentsQuery
            .Where(s => s.Status == StudentStatus.Active)
            .Select(s => s.Id)
            .ToListAsync();

        int count = 0;
        foreach (var studentId in students)
        {
            try
            {
                await CalculateStudentRiskAsync(studentId);
                count++;
            }
            catch (Exception)
            {
                // Log error but continue with other students
                // In production, use proper logging
                continue;
            }
        }

        return count;
    }

    public async Task<List<StudentRiskScore>> GetHighRiskStudentsAsync(Guid? facultyId = null, int limit = 20)
    {
        var query = _context.Set<StudentRiskScore>()
            .Include(r => r.Student)
                .ThenInclude(s => s.User)
            .Include(r => r.Student)
                .ThenInclude(s => s.Group)
                    .ThenInclude(g => g.Program)
                        .ThenInclude(p => p.Faculty)
            .Where(r => r.Level == RiskLevel.High || r.Level == RiskLevel.Critical);

        if (facultyId.HasValue)
        {
            query = query.Where(r =>
                r.Student.Group != null &&
                r.Student.Group.Program.FacultyId == facultyId.Value);
        }

        return await query
            .OrderByDescending(r => r.OverallScore)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<RiskDistributionDto> GetRiskDistributionAsync(Guid? facultyId = null)
    {
        var query = _context.Set<StudentRiskScore>()
            .Include(r => r.Student)
                .ThenInclude(s => s.Group)
                    .ThenInclude(g => g.Program)
            .AsQueryable();

        if (facultyId.HasValue)
        {
            query = query.Where(r =>
                r.Student.Group != null &&
                r.Student.Group.Program.FacultyId == facultyId.Value);
        }

        var scores = await query.ToListAsync();
        var total = scores.Count;

        if (total == 0)
        {
            return new RiskDistributionDto
            {
                TotalStudents = 0
            };
        }

        var lowCount = scores.Count(s => s.Level == RiskLevel.Low);
        var mediumCount = scores.Count(s => s.Level == RiskLevel.Medium);
        var highCount = scores.Count(s => s.Level == RiskLevel.High);
        var criticalCount = scores.Count(s => s.Level == RiskLevel.Critical);

        return new RiskDistributionDto
        {
            TotalStudents = total,
            LowRisk = lowCount,
            MediumRisk = mediumCount,
            HighRisk = highCount,
            CriticalRisk = criticalCount,
            LowRiskPercentage = Math.Round((decimal)lowCount / total * 100, 2),
            MediumRiskPercentage = Math.Round((decimal)mediumCount / total * 100, 2),
            HighRiskPercentage = Math.Round((decimal)highCount / total * 100, 2),
            CriticalRiskPercentage = Math.Round((decimal)criticalCount / total * 100, 2)
        };
    }

    // Helper Methods

    private RiskLevel DetermineRiskLevel(int score)
    {
        return score switch
        {
            <= THRESHOLD_LOW => RiskLevel.Low,
            <= THRESHOLD_MEDIUM => RiskLevel.Medium,
            <= THRESHOLD_HIGH => RiskLevel.High,
            _ => RiskLevel.Critical
        };
    }

    private int CalculateMaxConsecutiveAbsences(List<Attendance> attendances)
    {
        if (!attendances.Any())
            return 0;

        var sortedAbsences = attendances
            .Where(a => a.Status == AttendanceStatus.Absent)
            .OrderBy(a => a.Date)
            .Select(a => a.Date)
            .ToList();

        if (!sortedAbsences.Any())
            return 0;

        int maxConsecutive = 1;
        int currentConsecutive = 1;

        for (int i = 1; i < sortedAbsences.Count; i++)
        {
            var daysDiff = sortedAbsences[i].DayNumber - sortedAbsences[i - 1].DayNumber;
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

    private List<string> GetRiskFactorsList(RiskFactorsDto factors)
    {
        var list = new List<string>();

        if (factors.CurrentGradeScore > 50)
            list.Add($"Low average grade: {factors.Details.GetValueOrDefault("AverageGrade")}");

        if (factors.GradeTrendScore > 50)
            list.Add($"Declining grade trend: {factors.Details.GetValueOrDefault("GradeTrend")}");

        if (factors.AttendanceScore > 50)
            list.Add($"Poor attendance rate: {factors.Details.GetValueOrDefault("AttendanceRate")}");

        if (factors.ConsecutiveAbsencesScore > 50)
            list.Add($"Consecutive absences: {factors.Details.GetValueOrDefault("ConsecutiveAbsences")}");

        if (factors.FailedCoursesScore > 50)
            list.Add($"Failed courses: {factors.Details.GetValueOrDefault("FailedCourses")}");

        if (factors.EngagementScore > 50)
            list.Add("Low platform engagement");

        return list.Any() ? list : new List<string> { "No significant risk factors" };
    }
}
