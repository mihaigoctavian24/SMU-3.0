namespace SMU.Data.Entities;

/// <summary>
/// Analytics and predictive entities for data-driven insights
/// Supporting Faza 8: Predictive Analytics features
/// </summary>

/// <summary>
/// Daily snapshot of university-wide or faculty/program-specific metrics
/// Used for trend analysis and historical tracking
/// </summary>
public class DailySnapshot
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public Guid? FacultyId { get; set; }
    public Guid? ProgramId { get; set; }
    public int TotalStudents { get; set; }
    public int ActiveStudents { get; set; }
    public decimal? AverageGrade { get; set; }
    public decimal? AttendanceRate { get; set; }
    public int GradesSubmitted { get; set; }
    public int GradesApproved { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Faculty? Faculty { get; set; }
    public StudyProgram? StudyProgram { get; set; }
}

/// <summary>
/// Pre-calculated grade statistics per student and semester
/// Optimizes dashboard performance by caching computed metrics
/// </summary>
public class GradeSnapshot
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public int AcademicYear { get; set; }
    public int Semester { get; set; }
    public decimal? SemesterAverage { get; set; }
    public decimal? CumulativeAverage { get; set; }
    public int TotalCredits { get; set; }
    public int PassedCredits { get; set; }
    public int FailedCourses { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Student Student { get; set; } = null!;
}

/// <summary>
/// Aggregated attendance statistics per student and course
/// Tracks attendance patterns and identifies early warning signals
/// </summary>
public class AttendanceStats
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public int TotalClasses { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int ExcusedCount { get; set; }
    public decimal AttendanceRate { get; set; }
    public int ConsecutiveAbsences { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // Navigation
    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}

// Note: StudentRiskScore is defined in CoreEntities.cs and configured in ApplicationDbContext

/// <summary>
/// Daily tracking of student engagement with the platform
/// Measures interaction patterns and system usage
/// </summary>
public class StudentEngagement
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public DateOnly Date { get; set; }
    public int LoginCount { get; set; }
    public int GradesViewed { get; set; }
    public int AttendanceViewed { get; set; }
    public int DocumentsRequested { get; set; }
    public int MinutesActive { get; set; }

    // Navigation
    public Student Student { get; set; } = null!;
}

/// <summary>
/// Alert entity for risk-based notifications and interventions
/// Enables proactive academic support and early intervention
/// </summary>
public class RiskAlert
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsAcknowledged { get; set; }
    public Guid? AcknowledgedBy { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string? InterventionNotes { get; set; }
    public string? InterventionStatus { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Student Student { get; set; } = null!;
    public ApplicationUser? AcknowledgedByUser { get; set; }
}
