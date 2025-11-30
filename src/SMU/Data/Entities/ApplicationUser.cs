using Microsoft.AspNetCore.Identity;

namespace SMU.Data.Entities;

/// <summary>
/// Custom user entity extending ASP.NET Identity
/// Maps to asp_net_users table in Supabase
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Student;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public string? ProfileImageUrl { get; set; }

    // Navigation properties
    public Student? Student { get; set; }
    public Professor? Professor { get; set; }
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    // Computed properties
    public string FullName => $"{FirstName} {LastName}";
    public string Initials => $"{FirstName?.FirstOrDefault()}{LastName?.FirstOrDefault()}";
}

public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
