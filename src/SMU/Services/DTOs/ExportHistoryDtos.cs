using SMU.Data.Entities;

namespace SMU.Services.DTOs;

/// <summary>
/// DTO for export history display
/// </summary>
public class ExportHistoryDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public ExportType ExportType { get; set; }
    public string ExportTypeName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? Parameters { get; set; }
    public long FileSizeBytes { get; set; }
    public string FileSizeFormatted { get; set; } = string.Empty;
    public int DownloadCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedAtRelative { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
    public string? FilePath { get; set; }
}

/// <summary>
/// DTO for export statistics
/// </summary>
public class ExportStatsDto
{
    public int TotalExports { get; set; }
    public int TotalDownloads { get; set; }
    public long TotalFileSize { get; set; }
    public string TotalFileSizeFormatted { get; set; } = string.Empty;
    public Dictionary<ExportType, int> ExportsByType { get; set; } = new();
    public DateTime? MostRecentExport { get; set; }
    public ExportTypeStatsDto? MostPopularExport { get; set; }
}

/// <summary>
/// Statistics for a specific export type
/// </summary>
public class ExportTypeStatsDto
{
    public ExportType Type { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public int Count { get; set; }
    public int TotalDownloads { get; set; }
}
