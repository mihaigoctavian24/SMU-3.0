using SMU.Data.Entities;

namespace SMU.Services.DTOs;

/// <summary>
/// Filter criteria for activity log queries
/// </summary>
public class ActivityFilter
{
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public ActivityAction? Action { get; set; }
    public string? EntityType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchTerm { get; set; }
}

/// <summary>
/// DTO for activity log display
/// </summary>
public class ActivityLogDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public string? UserRole { get; set; }
    public string Action { get; set; } = string.Empty;
    public ActivityAction ActionType { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? EntityName { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }

    // UI Helper properties
    public string Icon { get; set; } = string.Empty;
    public string IconColor { get; set; } = string.Empty;
    public string RelativeTime { get; set; } = string.Empty;
    public string ActionDescription { get; set; } = string.Empty;
}

/// <summary>
/// DTO for exporting activity logs
/// </summary>
public class ActivityExportDto
{
    public string DateTime { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string IPAddress { get; set; } = string.Empty;
}
