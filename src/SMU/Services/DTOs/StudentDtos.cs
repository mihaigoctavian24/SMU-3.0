using SMU.Data.Entities;

namespace SMU.Services.DTOs;

/// <summary>
/// Filter criteria for student queries
/// </summary>
public class StudentFilter
{
    public string? Search { get; set; }
    public Guid? GroupId { get; set; }
    public Guid? ProgramId { get; set; }
    public Guid? FacultyId { get; set; }
    public StudentStatus? Status { get; set; }
}

/// <summary>
/// DTO for creating a new student
/// </summary>
public class CreateStudentDto
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public bool ScholarshipHolder { get; set; }
    public DateOnly EnrollmentDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
}

/// <summary>
/// DTO for updating student details
/// </summary>
public class UpdateStudentDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Guid? GroupId { get; set; }
    public bool ScholarshipHolder { get; set; }
    public StudentStatus Status { get; set; }
}

/// <summary>
/// DTO for student list display
/// </summary>
public class StudentListDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string StudentNumber { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string ProgramName { get; set; } = string.Empty;
    public StudentStatus Status { get; set; }
}

/// <summary>
/// DTO for detailed student view
/// </summary>
public class StudentDetailDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string StudentNumber { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }

    public Guid? GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string ProgramName { get; set; } = string.Empty;
    public string FacultyName { get; set; } = string.Empty;
    public int Year { get; set; }

    public StudentStatus Status { get; set; }
    public bool ScholarshipHolder { get; set; }
    public DateOnly EnrollmentDate { get; set; }

    public int GradesCount { get; set; }
    public decimal? AverageGrade { get; set; }
    public decimal AttendanceRate { get; set; }
    public int TotalCredits { get; set; }
}

/// <summary>
/// Generic paged result wrapper
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
