using SMU.Data.Entities;

namespace SMU.Services.DTOs;

/// <summary>
/// Filter criteria for group queries
/// </summary>
public class GroupFilter
{
    public string? Search { get; set; }
    public Guid? ProgramId { get; set; }
    public Guid? FacultyId { get; set; }
    public int? Year { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// DTO for creating a new group
/// </summary>
public class CreateGroupDto
{
    public string Name { get; set; } = string.Empty;
    public Guid ProgramId { get; set; }
    public int Year { get; set; } = 1;
    public int MaxStudents { get; set; } = 30;
}

/// <summary>
/// DTO for updating group details
/// </summary>
public class UpdateGroupDto
{
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }
    public int MaxStudents { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for group list display
/// </summary>
public class GroupListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public Guid ProgramId { get; set; }
    public string FacultyName { get; set; } = string.Empty;
    public int StudentsCount { get; set; }
    public int MaxStudents { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for detailed group view
/// </summary>
public class GroupDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }
    public Guid ProgramId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public string FacultyName { get; set; } = string.Empty;
    public int MaxStudents { get; set; }
    public int StudentsCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
