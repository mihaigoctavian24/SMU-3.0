using SMU.Data.Entities;

namespace SMU.Services.DTOs;

/// <summary>
/// Filter criteria for faculty queries
/// </summary>
public class FacultyFilter
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// DTO for creating a new faculty
/// </summary>
public class CreateFacultyDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? DeanId { get; set; }
}

/// <summary>
/// DTO for updating faculty details
/// </summary>
public class UpdateFacultyDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? DeanId { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for faculty list display
/// </summary>
public class FacultyListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? DeanName { get; set; }
    public int ProgramsCount { get; set; }
    public int StudentsCount { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for detailed faculty view
/// </summary>
public class FacultyDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? DeanId { get; set; }
    public string? DeanName { get; set; }
    public string? DeanEmail { get; set; }
    public bool IsActive { get; set; }
    public int ProgramsCount { get; set; }
    public int ProfessorsCount { get; set; }
    public int StudentsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
