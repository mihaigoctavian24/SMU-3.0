using SMU.Data.Entities;

namespace SMU.Services.DTOs;

/// <summary>
/// Filter criteria for study program queries
/// </summary>
public class StudyProgramFilter
{
    public string? Search { get; set; }
    public Guid? FacultyId { get; set; }
    public ProgramType? Type { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// DTO for creating a new study program
/// </summary>
public class CreateStudyProgramDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Guid FacultyId { get; set; }
    public ProgramType Type { get; set; } = ProgramType.Bachelor;
    public int DurationYears { get; set; } = 3;
    public int TotalCredits { get; set; } = 180;
    public string? Description { get; set; }
}

/// <summary>
/// DTO for updating study program details
/// </summary>
public class UpdateStudyProgramDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public ProgramType Type { get; set; }
    public int DurationYears { get; set; }
    public int TotalCredits { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for study program list display
/// </summary>
public class StudyProgramListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string FacultyName { get; set; } = string.Empty;
    public Guid FacultyId { get; set; }
    public ProgramType Type { get; set; }
    public int DurationYears { get; set; }
    public int GroupsCount { get; set; }
    public int StudentsCount { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for detailed study program view
/// </summary>
public class StudyProgramDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Guid FacultyId { get; set; }
    public string FacultyName { get; set; } = string.Empty;
    public ProgramType Type { get; set; }
    public int DurationYears { get; set; }
    public int TotalCredits { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int GroupsCount { get; set; }
    public int CoursesCount { get; set; }
    public int StudentsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
