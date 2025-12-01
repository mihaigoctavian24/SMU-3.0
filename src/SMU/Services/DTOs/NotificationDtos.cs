using SMU.Data.Entities;

namespace SMU.Services.DTOs;

/// <summary>
/// DTO for notification display
/// </summary>
public class NotificationDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string TypeLabel { get; set; } = string.Empty;
    public string TypeIcon { get; set; } = string.Empty;
    public string TypeColor { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public string? Link { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TimeAgo { get; set; } = string.Empty;
}

/// <summary>
/// DTO for creating a new notification
/// </summary>
public class CreateNotificationDto
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public string? Link { get; set; }
}

/// <summary>
/// Filter criteria for notification queries
/// </summary>
public class NotificationFilter
{
    public Guid? UserId { get; set; }
    public bool UnreadOnly { get; set; }
    public NotificationType? Type { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
