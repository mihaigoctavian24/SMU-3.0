using Microsoft.EntityFrameworkCore;
using SMU.Data;
using SMU.Data.Entities;
using SMU.Services.DTOs;
using ClosedXML.Excel;

namespace SMU.Services;

public class ActivityFeedService : IActivityFeedService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ActivityFeedService> _logger;

    public ActivityFeedService(ApplicationDbContext context, ILogger<ActivityFeedService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ActivityLogDto>> GetRecentActivitiesAsync(int limit = 50, ActivityFilter? filter = null)
    {
        var query = _context.AuditLogs
            .Include(a => a.User)
            .AsQueryable();

        query = ApplyFilters(query, filter);

        var activities = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return MapToDto(activities);
    }

    public async Task<PagedResult<ActivityLogDto>> GetPagedActivitiesAsync(ActivityFilter filter, int page = 1, int pageSize = 25)
    {
        var query = _context.AuditLogs
            .Include(a => a.User)
            .AsQueryable();

        query = ApplyFilters(query, filter);

        var totalCount = await query.CountAsync();

        var activities = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ActivityLogDto>
        {
            Items = MapToDto(activities),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<ActivityLogDto>> GetActivitiesByUserAsync(Guid userId, int limit = 50)
    {
        var activities = await _context.AuditLogs
            .Include(a => a.User)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return MapToDto(activities);
    }

    public async Task<List<ActivityLogDto>> GetActivitiesByEntityAsync(string entityType, Guid entityId)
    {
        var activities = await _context.AuditLogs
            .Include(a => a.User)
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return MapToDto(activities);
    }

    public async Task LogActivityAsync(Guid? userId, ActivityAction action, string entityType,
        Guid? entityId = null, string? entityName = null, string? oldValues = null,
        string? newValues = null, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = action.ToString(),
                EntityType = entityType,
                EntityId = entityId,
                OldValues = oldValues,
                NewValues = newValues,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Activity logged: {Action} on {EntityType} by {UserId}",
                action, entityType, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log activity: {Action} on {EntityType}", action, entityType);
        }
    }

    public async Task<byte[]> ExportActivitiesAsync(ActivityFilter filter)
    {
        var query = _context.AuditLogs
            .Include(a => a.User)
            .AsQueryable();

        query = ApplyFilters(query, filter);

        var activities = await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Activity Log");

        // Headers
        worksheet.Cell(1, 1).Value = "Data/Ora";
        worksheet.Cell(1, 2).Value = "Utilizator";
        worksheet.Cell(1, 3).Value = "Rol";
        worksheet.Cell(1, 4).Value = "Acțiune";
        worksheet.Cell(1, 5).Value = "Tip Entitate";
        worksheet.Cell(1, 6).Value = "Detalii";
        worksheet.Cell(1, 7).Value = "Adresă IP";

        // Style headers
        var headerRange = worksheet.Range(1, 1, 1, 7);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // Data
        int row = 2;
        foreach (var activity in activities)
        {
            worksheet.Cell(row, 1).Value = activity.CreatedAt.ToString("dd.MM.yyyy HH:mm:ss");
            worksheet.Cell(row, 2).Value = activity.User != null
                ? $"{activity.User.FirstName} {activity.User.LastName}"
                : "System";
            worksheet.Cell(row, 3).Value = activity.User?.Role.ToString() ?? "System";
            worksheet.Cell(row, 4).Value = TranslateAction(activity.Action);
            worksheet.Cell(row, 5).Value = TranslateEntityType(activity.EntityType);
            worksheet.Cell(row, 6).Value = GetEntityDescription(activity);
            worksheet.Cell(row, 7).Value = activity.IpAddress ?? "-";
            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<ActivityStatsDto> GetActivityStatsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        startDate ??= DateTime.UtcNow.AddDays(-30);
        endDate ??= DateTime.UtcNow;

        var query = _context.AuditLogs
            .Include(a => a.User)
            .Where(a => a.CreatedAt >= startDate && a.CreatedAt <= endDate);

        var totalActivities = await query.CountAsync();
        var uniqueUsers = await query.Where(a => a.UserId != null).Select(a => a.UserId).Distinct().CountAsync();
        var todayStart = DateTime.UtcNow.Date;
        var todayActivities = await query.Where(a => a.CreatedAt >= todayStart).CountAsync();

        var actionCounts = await query
            .GroupBy(a => a.Action)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Action, x => x.Count);

        var entityTypeCounts = await query
            .GroupBy(a => a.EntityType)
            .Select(g => new { EntityType = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EntityType, x => x.Count);

        var topUsers = await query
            .Where(a => a.User != null)
            .GroupBy(a => new { a.UserId, a.User!.FirstName, a.User.LastName, a.User.Role })
            .Select(g => new TopUserActivity
            {
                UserName = $"{g.Key.FirstName} {g.Key.LastName}",
                Role = g.Key.Role.ToString(),
                ActivityCount = g.Count()
            })
            .OrderByDescending(x => x.ActivityCount)
            .Take(10)
            .ToListAsync();

        return new ActivityStatsDto
        {
            TotalActivities = totalActivities,
            UniqueUsers = uniqueUsers,
            TodayActivities = todayActivities,
            ActionCounts = actionCounts,
            EntityTypeCounts = entityTypeCounts,
            TopUsers = topUsers
        };
    }

    // Helper methods
    private IQueryable<AuditLog> ApplyFilters(IQueryable<AuditLog> query, ActivityFilter? filter)
    {
        if (filter == null) return query;

        if (filter.UserId.HasValue)
            query = query.Where(a => a.UserId == filter.UserId.Value);

        if (filter.Action.HasValue)
            query = query.Where(a => a.Action == filter.Action.Value.ToString());

        if (!string.IsNullOrWhiteSpace(filter.EntityType))
            query = query.Where(a => a.EntityType == filter.EntityType);

        if (filter.StartDate.HasValue)
            query = query.Where(a => a.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(a => a.CreatedAt <= filter.EndDate.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchLower = filter.SearchTerm.ToLower();
            query = query.Where(a =>
                a.User!.FirstName.ToLower().Contains(searchLower) ||
                a.User.LastName.ToLower().Contains(searchLower) ||
                a.User.Email!.ToLower().Contains(searchLower) ||
                a.EntityType.ToLower().Contains(searchLower) ||
                a.Action.ToLower().Contains(searchLower));
        }

        return query;
    }

    private List<ActivityLogDto> MapToDto(List<AuditLog> activities)
    {
        return activities.Select(a =>
        {
            var action = ParseAction(a.Action);
            return new ActivityLogDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.User != null ? $"{a.User.FirstName} {a.User.LastName}" : "System",
                UserEmail = a.User?.Email,
                UserRole = a.User?.Role.ToString(),
                Action = a.Action,
                ActionType = action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                EntityName = GetEntityName(a),
                Details = GetEntityDescription(a),
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent,
                CreatedAt = a.CreatedAt,
                Icon = GetActionIcon(action),
                IconColor = GetActionColor(action),
                RelativeTime = GetRelativeTime(a.CreatedAt),
                ActionDescription = BuildActionDescription(a, action)
            };
        }).ToList();
    }

    private ActivityAction ParseAction(string action)
    {
        return Enum.TryParse<ActivityAction>(action, out var result) ? result : ActivityAction.Viewed;
    }

    private string GetActionIcon(ActivityAction action)
    {
        return action switch
        {
            ActivityAction.Created => "plus-circle",
            ActivityAction.Updated => "pencil",
            ActivityAction.Deleted => "trash",
            ActivityAction.Viewed => "eye",
            ActivityAction.Exported => "download",
            ActivityAction.LoggedIn => "log-in",
            ActivityAction.LoggedOut => "log-out",
            ActivityAction.Approved => "check-circle",
            ActivityAction.Rejected => "x-circle",
            ActivityAction.Assigned => "user-plus",
            _ => "activity"
        };
    }

    private string GetActionColor(ActivityAction action)
    {
        return action switch
        {
            ActivityAction.Created => "green",
            ActivityAction.Updated => "blue",
            ActivityAction.Deleted => "red",
            ActivityAction.Viewed => "gray",
            ActivityAction.Exported => "purple",
            ActivityAction.LoggedIn => "green",
            ActivityAction.LoggedOut => "gray",
            ActivityAction.Approved => "green",
            ActivityAction.Rejected => "red",
            ActivityAction.Assigned => "blue",
            _ => "gray"
        };
    }

    private string GetRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        if (timeSpan.TotalMinutes < 1) return "acum";
        if (timeSpan.TotalMinutes < 60) return $"acum {(int)timeSpan.TotalMinutes} min";
        if (timeSpan.TotalHours < 24) return $"acum {(int)timeSpan.TotalHours} ore";
        if (timeSpan.TotalDays < 7) return $"acum {(int)timeSpan.TotalDays} zile";
        return dateTime.ToString("dd.MM.yyyy HH:mm");
    }

    private string BuildActionDescription(AuditLog log, ActivityAction action)
    {
        var entityType = TranslateEntityType(log.EntityType);
        var actionText = TranslateAction(log.Action);

        return $"{actionText} {entityType}";
    }

    private string GetEntityName(AuditLog log)
    {
        // Try to extract entity name from NewValues JSON if available
        if (!string.IsNullOrEmpty(log.NewValues))
        {
            try
            {
                var json = System.Text.Json.JsonDocument.Parse(log.NewValues);
                if (json.RootElement.TryGetProperty("Name", out var name))
                    return name.GetString() ?? log.EntityId?.ToString() ?? "";
                if (json.RootElement.TryGetProperty("FirstName", out var firstName) &&
                    json.RootElement.TryGetProperty("LastName", out var lastName))
                    return $"{firstName.GetString()} {lastName.GetString()}";
            }
            catch { }
        }
        return log.EntityId?.ToString() ?? "";
    }

    private string GetEntityDescription(AuditLog log)
    {
        var entityName = GetEntityName(log);
        return string.IsNullOrEmpty(entityName) ? "-" : entityName;
    }

    private string TranslateAction(string action)
    {
        return action switch
        {
            "Created" => "Creat",
            "Updated" => "Modificat",
            "Deleted" => "Șters",
            "Viewed" => "Vizualizat",
            "Exported" => "Exportat",
            "LoggedIn" => "Autentificare",
            "LoggedOut" => "Deconectare",
            "Approved" => "Aprobat",
            "Rejected" => "Respins",
            "Assigned" => "Asignat",
            _ => action
        };
    }

    private string TranslateEntityType(string entityType)
    {
        return entityType switch
        {
            "Student" => "Student",
            "Grade" => "Notă",
            "Course" => "Curs",
            "User" => "Utilizator",
            "Faculty" => "Facultate",
            "Program" => "Program",
            "Group" => "Grupă",
            "Attendance" => "Prezență",
            "DocumentRequest" => "Cerere Document",
            "Professor" => "Profesor",
            _ => entityType
        };
    }
}
