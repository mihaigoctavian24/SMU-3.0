using SMU.Data.Entities;

namespace SMU.Services.DTOs;

/// <summary>
/// Filter criteria for user queries
/// </summary>
public class UserFilter
{
    public UserRole? Role { get; set; }
    public bool? IsActive { get; set; }
    public string? Search { get; set; }
}

/// <summary>
/// DTO for user list display
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public UserRole Role { get; set; }
    public string RoleLabel => GetRoleLabel(Role);
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public Guid? StudentId { get; set; }
    public Guid? ProfessorId { get; set; }

    private static string GetRoleLabel(UserRole role) => role switch
    {
        UserRole.Admin => "Administrator",
        UserRole.Rector => "Rector",
        UserRole.Dean => "Decan",
        UserRole.Secretary => "Secretar",
        UserRole.Professor => "Profesor",
        UserRole.Student => "Student",
        _ => role.ToString()
    };
}

/// <summary>
/// DTO for creating a new user
/// </summary>
public class CreateUserDto
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid? FacultyId { get; set; }  // For linking professor to faculty
}

/// <summary>
/// DTO for updating user details
/// </summary>
public class UpdateUserDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for role statistics
/// </summary>
public class RoleStatsDto
{
    public UserRole Role { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int UserCount { get; set; }
}
