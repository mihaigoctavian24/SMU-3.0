using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SMU.Services.DTOs;

namespace SMU.Hubs;

/// <summary>
/// SignalR Hub for real-time notifications
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when client connects - join user-specific group
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} connected to notification hub", userId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when client disconnects - leave user-specific group
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} disconnected from notification hub", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a specific user group (called explicitly from client if needed)
    /// </summary>
    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        _logger.LogInformation("Connection {ConnectionId} joined group for user {UserId}",
            Context.ConnectionId, userId);
    }

    /// <summary>
    /// Leave a specific user group (called explicitly from client if needed)
    /// </summary>
    public async Task LeaveUserGroup(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        _logger.LogInformation("Connection {ConnectionId} left group for user {UserId}",
            Context.ConnectionId, userId);
    }

    /// <summary>
    /// Send notification to a specific user via their group
    /// </summary>
    public static async Task SendNotification(
        IHubContext<NotificationHub> hubContext,
        Guid userId,
        NotificationDto notification)
    {
        await hubContext.Clients
            .Group($"user_{userId}")
            .SendAsync("ReceiveNotification", notification);
    }

    /// <summary>
    /// Broadcast notification count update to a user
    /// </summary>
    public static async Task SendUnreadCountUpdate(
        IHubContext<NotificationHub> hubContext,
        Guid userId,
        int unreadCount)
    {
        await hubContext.Clients
            .Group($"user_{userId}")
            .SendAsync("UpdateUnreadCount", unreadCount);
    }

    /// <summary>
    /// Join admin activity feed group (for real-time activity updates)
    /// </summary>
    public async Task JoinAdminActivityGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "admin_activity");
        _logger.LogInformation("Connection {ConnectionId} joined admin activity group", Context.ConnectionId);
    }

    /// <summary>
    /// Leave admin activity feed group
    /// </summary>
    public async Task LeaveAdminActivityGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "admin_activity");
        _logger.LogInformation("Connection {ConnectionId} left admin activity group", Context.ConnectionId);
    }

    /// <summary>
    /// Broadcast new activity to admin activity feed
    /// </summary>
    public static async Task SendActivityUpdate(
        IHubContext<NotificationHub> hubContext,
        ActivityLogDto activity)
    {
        await hubContext.Clients
            .Group("admin_activity")
            .SendAsync("ReceiveActivity", activity);
    }
}
