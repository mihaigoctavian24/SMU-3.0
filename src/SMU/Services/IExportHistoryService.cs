using SMU.Data.Entities;
using SMU.Services.DTOs;

namespace SMU.Services;

/// <summary>
/// Service interface for managing export history
/// Tracks all document exports for audit, re-download, and analytics
/// </summary>
public interface IExportHistoryService
{
    /// <summary>
    /// Log a new export to history
    /// </summary>
    /// <param name="userId">User who generated the export</param>
    /// <param name="exportType">Type of export</param>
    /// <param name="fileName">Generated file name</param>
    /// <param name="fileSize">File size in bytes</param>
    /// <param name="parameters">Export parameters as object (will be serialized to JSON)</param>
    /// <param name="filePath">Optional: path to stored file for re-download</param>
    /// <returns>Export history ID</returns>
    Task<Guid> LogExportAsync(
        Guid userId,
        ExportType exportType,
        string fileName,
        long fileSize,
        object? parameters = null,
        string? filePath = null);

    /// <summary>
    /// Get export history for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="limit">Maximum number of records to return (default 20)</param>
    /// <param name="exportType">Optional: filter by export type</param>
    /// <returns>List of export history DTOs</returns>
    Task<List<ExportHistoryDto>> GetUserExportsAsync(
        Guid userId,
        int limit = 20,
        ExportType? exportType = null);

    /// <summary>
    /// Get all export history (Admin only)
    /// </summary>
    /// <param name="limit">Maximum number of records to return (default 100)</param>
    /// <param name="exportType">Optional: filter by export type</param>
    /// <returns>List of export history DTOs</returns>
    Task<List<ExportHistoryDto>> GetAllExportsAsync(
        int limit = 100,
        ExportType? exportType = null);

    /// <summary>
    /// Get a specific export by ID
    /// </summary>
    /// <param name="exportId">Export history ID</param>
    /// <returns>Export history DTO or null if not found</returns>
    Task<ExportHistoryDto?> GetExportAsync(Guid exportId);

    /// <summary>
    /// Increment download count for an export
    /// Called each time user downloads an export
    /// </summary>
    /// <param name="exportId">Export history ID</param>
    Task IncrementDownloadCountAsync(Guid exportId);

    /// <summary>
    /// Soft delete an export from history
    /// </summary>
    /// <param name="exportId">Export history ID</param>
    Task DeleteExportAsync(Guid exportId);

    /// <summary>
    /// Permanently remove expired exports
    /// Should be run as a background job
    /// </summary>
    /// <param name="daysOld">Number of days before considering an export expired (default 30)</param>
    /// <returns>Number of records cleaned up</returns>
    Task<int> CleanupExpiredExportsAsync(int daysOld = 30);

    /// <summary>
    /// Get export statistics for analytics
    /// </summary>
    /// <param name="userId">Optional: filter by user</param>
    /// <param name="facultyId">Optional: filter by faculty</param>
    /// <returns>Export statistics</returns>
    Task<ExportStatsDto> GetExportStatsAsync(Guid? userId = null, Guid? facultyId = null);
}
