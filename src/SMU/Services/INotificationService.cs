using SMU.Data.Entities;
using SMU.Services.DTOs;

namespace SMU.Services;

/// <summary>
/// Service interface for notification management
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Get notifications for a specific user
    /// </summary>
    Task<List<NotificationDto>> GetByUserAsync(Guid userId, bool unreadOnly = false);

    /// <summary>
    /// Get unread notification count for a user
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid userId);

    /// <summary>
    /// Get recent notifications (last N) for a user
    /// </summary>
    Task<List<NotificationDto>> GetRecentAsync(Guid userId, int count = 10);

    /// <summary>
    /// Get paginated notifications for a user
    /// </summary>
    Task<PagedResult<NotificationDto>> GetPagedAsync(Guid userId, NotificationFilter filter, int page = 1, int pageSize = 25);

    /// <summary>
    /// Get notification by ID
    /// </summary>
    Task<NotificationDto?> GetByIdAsync(Guid notificationId);

    /// <summary>
    /// Create a new notification
    /// </summary>
    Task<ServiceResult<Guid>> CreateAsync(CreateNotificationDto dto);

    /// <summary>
    /// Mark a specific notification as read
    /// </summary>
    Task<ServiceResult> MarkAsReadAsync(Guid notificationId);

    /// <summary>
    /// Mark all notifications as read for a user
    /// </summary>
    Task<ServiceResult> MarkAllAsReadAsync(Guid userId);

    /// <summary>
    /// Delete a specific notification
    /// </summary>
    Task<ServiceResult> DeleteAsync(Guid notificationId);

    /// <summary>
    /// Send a notification to a user (creates and optionally broadcasts via SignalR)
    /// </summary>
    Task SendAsync(Guid userId, string title, string message, NotificationType type, string? link = null);
}
