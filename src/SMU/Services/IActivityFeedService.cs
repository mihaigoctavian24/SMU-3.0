using SMU.Data.Entities;
using SMU.Services.DTOs;

namespace SMU.Services;

/// <summary>
/// Service interface for activity feed and audit log management
/// </summary>
public interface IActivityFeedService
{
    /// <summary>
    /// Get recent activities with optional filters
    /// </summary>
    Task<List<ActivityLogDto>> GetRecentActivitiesAsync(int limit = 50, ActivityFilter? filter = null);

    /// <summary>
    /// Get paginated activities with filters
    /// </summary>
    Task<PagedResult<ActivityLogDto>> GetPagedActivitiesAsync(ActivityFilter filter, int page = 1, int pageSize = 25);

    /// <summary>
    /// Get activities by specific user
    /// </summary>
    Task<List<ActivityLogDto>> GetActivitiesByUserAsync(Guid userId, int limit = 50);

    /// <summary>
    /// Get activities by entity type and ID
    /// </summary>
    Task<List<ActivityLogDto>> GetActivitiesByEntityAsync(string entityType, Guid entityId);

    /// <summary>
    /// Log a new activity
    /// </summary>
    Task LogActivityAsync(Guid? userId, ActivityAction action, string entityType, Guid? entityId = null,
        string? entityName = null, string? oldValues = null, string? newValues = null,
        string? ipAddress = null, string? userAgent = null);

    /// <summary>
    /// Export activities to Excel
    /// </summary>
    Task<byte[]> ExportActivitiesAsync(ActivityFilter filter);

    /// <summary>
    /// Get activity statistics for dashboard
    /// </summary>
    Task<ActivityStatsDto> GetActivityStatsAsync(DateTime? startDate = null, DateTime? endDate = null);
}

/// <summary>
/// Activity statistics DTO
/// </summary>
public class ActivityStatsDto
{
    public int TotalActivities { get; set; }
    public int UniqueUsers { get; set; }
    public int TodayActivities { get; set; }
    public Dictionary<string, int> ActionCounts { get; set; } = new();
    public Dictionary<string, int> EntityTypeCounts { get; set; } = new();
    public List<TopUserActivity> TopUsers { get; set; } = new();
}

public class TopUserActivity
{
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int ActivityCount { get; set; }
}
