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
    public string CourseName { get; set; } = string.Empty;
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
}
