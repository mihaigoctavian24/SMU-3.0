namespace SMU.Services.DTOs;

/// <summary>
/// DTO for creating a new grade
/// </summary>
public class CreateGradeDto
{
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public decimal Value { get; set; }
    public Data.Entities.GradeType Type { get; set; }
    public DateOnly ExamDate { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for updating an existing grade
/// </summary>
public class UpdateGradeDto
{
    public decimal Value { get; set; }
    public Data.Entities.GradeType Type { get; set; }
    public DateOnly ExamDate { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for listing grades in tables
/// </summary>
public class GradeListDto
{
    public Guid Id { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentNumber { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string ProfessorName { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public Data.Entities.GradeType Type { get; set; }
    public Data.Entities.GradeStatus Status { get; set; }
    public DateOnly ExamDate { get; set; }
    public string? EnteredByName { get; set; }
    public int Credits { get; set; }
    public int Semester { get; set; }
}

/// <summary>
/// DTO for detailed grade information
/// </summary>
public class GradeDetailDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentNumber { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public int Credits { get; set; }
    public decimal Value { get; set; }
    public Data.Entities.GradeType Type { get; set; }
    public Data.Entities.GradeStatus Status { get; set; }
    public DateOnly ExamDate { get; set; }
    public string? Notes { get; set; }
    public string? EnteredByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for bulk grade creation
/// </summary>
public class BulkGradeDto
{
    public Guid CourseId { get; set; }
    public List<BulkGradeItemDto> Grades { get; set; } = new();
}

/// <summary>
/// Individual grade item in bulk operation
/// </summary>
public class BulkGradeItemDto
{
    public Guid StudentId { get; set; }
    public decimal Value { get; set; }
    public Data.Entities.GradeType Type { get; set; }
}

/// <summary>
/// DTO for grade approval/rejection
/// </summary>
public class GradeApprovalDto
{
    public Guid GradeId { get; set; }
    public string Action { get; set; } = string.Empty; // "approve" or "reject"
    public string? Reason { get; set; }
}

/// <summary>
/// DTO for student average calculation
/// </summary>
public class StudentAverageDto
{
    public Guid StudentId { get; set; }
    public decimal WeightedAverage { get; set; }
    public int TotalCredits { get; set; }
    public int CompletedCourses { get; set; }
    public int PassedGrades { get; set; }
    public int FailedGrades { get; set; }
}

/// <summary>
/// DTO for filtering grades with comprehensive criteria
/// </summary>
public class GradeFilterDto
{
    public Guid? StudentId { get; set; }
    public Guid? CourseId { get; set; }
    public Guid? ProfessorId { get; set; }
    public Guid? AcademicYearId { get; set; }
    public int? Semester { get; set; }
    public Data.Entities.GradeStatus? Status { get; set; }
    public Data.Entities.GradeType? Type { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

/// <summary>
/// DTO for grade statistics dashboard
/// </summary>
public class GradeStatisticsDto
{
    public int TotalGrades { get; set; }
    public int PendingGrades { get; set; }
    public int ApprovedToday { get; set; }
    public int ApprovedThisWeek { get; set; }
    public decimal AverageGrade { get; set; }
    public int FailingGrades { get; set; }
    public Dictionary<Data.Entities.GradeType, int> GradesByType { get; set; } = new();
}

/// <summary>
/// DTO for missing grades detection
/// </summary>
public class MissingGradeDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentNumber { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
}

/// <summary>
/// DTO for grade history tracking (audit trail)
/// </summary>
public class GradeHistoryDto
{
    public Guid GradeId { get; set; }
    public string Action { get; set; } = string.Empty; // Created, Updated, Approved, Rejected
    public DateTime Timestamp { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Notes { get; set; }
}
