namespace SMU.Data.Entities;

/// <summary>
/// Export history record tracking all document exports
/// Enables re-download, audit, and analytics for exports
/// </summary>
public class ExportHistory
{
    public Guid Id { get; set; }

    /// <summary>
    /// User who generated the export
    /// </summary>
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Type of export (PDF, Excel, etc.)
    /// </summary>
    public ExportType ExportType { get; set; }

    /// <summary>
    /// Generated file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Export parameters as JSON (for re-generation)
    /// Example: {"studentId": "guid", "purpose": "pentru bursă"}
    /// </summary>
    public string? Parameters { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Number of times this export was downloaded
    /// </summary>
    public int DownloadCount { get; set; }

    /// <summary>
    /// Optional: File path for persistent storage
    /// If null, export must be regenerated on-demand
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// When the export was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional expiration date for automatic cleanup
    /// Defaults to 30 days from creation
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether this export has been deleted (soft delete)
    /// </summary>
    public bool IsDeleted { get; set; }
}
