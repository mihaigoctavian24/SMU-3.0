using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SMU.Data;
using SMU.Data.Entities;
using SMU.Hubs;
using SMU.Services.DTOs;

namespace SMU.Services;

/// <summary>
/// Notification management service implementation
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationService> _logger;
    private readonly IHubContext<NotificationHub>? _hubContext;

    public NotificationService(
        ApplicationDbContext context,
        ILogger<NotificationService> logger,
        IHubContext<NotificationHub>? hubContext = null)
    {
        _context = context;
        _logger = logger;
        _hubContext = hubContext;
    }

    public async Task<List<NotificationDto>> GetByUserAsync(Guid userId, bool unreadOnly = false)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notifications.Select(MapToDto).ToList();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync();
    }

    public async Task<List<NotificationDto>> GetRecentAsync(Guid userId, int count = 10)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(count)
            .ToListAsync();

        return notifications.Select(MapToDto).ToList();
    }

    public async Task<PagedResult<NotificationDto>> GetPagedAsync(
        Guid userId,
        NotificationFilter filter,
        int page = 1,
        int pageSize = 25)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId)
            .AsQueryable();

        // Apply filters
        if (filter.UnreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(n => n.Type == filter.Type.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(n => n.CreatedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(n => n.CreatedAt <= filter.ToDate.Value);
        }

        var totalCount = await query.CountAsync();

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<NotificationDto>
        {
            Items = notifications.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<NotificationDto?> GetByIdAsync(Guid notificationId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId);

        return notification != null ? MapToDto(notification) : null;
    }

    public async Task<ServiceResult<Guid>> CreateAsync(CreateNotificationDto dto)
    {
        try
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = dto.UserId,
                Title = dto.Title,
                Message = dto.Message,
                Type = dto.Type,
                Link = dto.Link,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Notification created: {NotificationId} for user {UserId}", notification.Id, dto.UserId);

            return ServiceResult<Guid>.Success(notification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification for user {UserId}", dto.UserId);
            return ServiceResult<Guid>.Failed("Eroare la crearea notificării.");
        }
    }

    public async Task<ServiceResult> MarkAsReadAsync(Guid notificationId)
    {
        try
        {
            var notification = await _context.Notifications.FindAsync(notificationId);

            if (notification == null)
            {
                return ServiceResult.Failed("Notificarea nu a fost găsită.");
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
            return ServiceResult.Failed("Eroare la marcarea notificării ca citită.");
        }
    }

    public async Task<ServiceResult> MarkAllAsReadAsync(Guid userId)
    {
        try
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Marked {Count} notifications as read for user {UserId}",
                unreadNotifications.Count, userId);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            return ServiceResult.Failed("Eroare la marcarea notificărilor ca citite.");
        }
    }

    public async Task<ServiceResult> DeleteAsync(Guid notificationId)
    {
        try
        {
            var notification = await _context.Notifications.FindAsync(notificationId);

            if (notification == null)
            {
                return ServiceResult.Failed("Notificarea nu a fost găsită.");
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Notification deleted: {NotificationId}", notificationId);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId}", notificationId);
            return ServiceResult.Failed("Eroare la ștergerea notificării.");
        }
    }

    public async Task SendAsync(Guid userId, string title, string message, NotificationType type, string? link = null)
    {
        try
        {
            // Create notification in database
            var createDto = new CreateNotificationDto
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                Link = link
            };

            var result = await CreateAsync(createDto);

            if (result.Succeeded && result.Data != Guid.Empty)
            {
                // Send real-time notification via SignalR if available
                if (_hubContext != null)
                {
                    var notification = await GetByIdAsync(result.Data);
                    if (notification != null)
                    {
                        await NotificationHub.SendNotification(_hubContext, userId, notification);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
        }
    }

    private static NotificationDto MapToDto(Notification notification)
    {
        var (label, icon, color) = GetNotificationTypeInfo(notification.Type);

        return new NotificationDto
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            TypeLabel = label,
            TypeIcon = icon,
            TypeColor = color,
            IsRead = notification.IsRead,
            Link = notification.Link,
            CreatedAt = notification.CreatedAt,
            TimeAgo = GetTimeAgo(notification.CreatedAt)
        };
    }

    private static (string Label, string Icon, string Color) GetNotificationTypeInfo(NotificationType type)
    {
        return type switch
        {
            NotificationType.Info => ("Informație", "ℹ️", "blue"),
            NotificationType.Success => ("Succes", "✅", "green"),
            NotificationType.Warning => ("Avertisment", "⚠️", "yellow"),
            NotificationType.Error => ("Eroare", "❌", "red"),
            NotificationType.GradeAdded => ("Notă Nouă", "📝", "purple"),
            NotificationType.AttendanceMarked => ("Prezență", "📋", "cyan"),
            NotificationType.RequestUpdate => ("Cerere Procesată", "📄", "indigo"),
            _ => ("Notificare", "🔔", "gray")
        };
    }

    private static string GetTimeAgo(DateTime createdAt)
    {
        var timeSpan = DateTime.UtcNow - createdAt;

        if (timeSpan.TotalMinutes < 1)
            return "Acum";
        if (timeSpan.TotalMinutes < 60)
            return $"Acum {(int)timeSpan.TotalMinutes} minute";
        if (timeSpan.TotalHours < 24)
            return $"Acum {(int)timeSpan.TotalHours} ore";
        if (timeSpan.TotalDays < 7)
            return $"Acum {(int)timeSpan.TotalDays} zile";
        if (timeSpan.TotalDays < 30)
            return $"Acum {(int)(timeSpan.TotalDays / 7)} săptămâni";

        return createdAt.ToString("dd MMM yyyy", new System.Globalization.CultureInfo("ro-RO"));
    }
}

// ServiceResult classes are defined in CourseService.cs
