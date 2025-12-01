using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SMU.Data;
using SMU.Data.Entities;
using SMU.Services.DTOs;

namespace SMU.Services;

/// <summary>
/// Service for managing export history
/// Tracks all document exports for audit, re-download, and analytics
/// </summary>
public class ExportHistoryService : IExportHistoryService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ExportHistoryService> _logger;

    public ExportHistoryService(
        ApplicationDbContext context,
        ILogger<ExportHistoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Guid> LogExportAsync(
        Guid userId,
        ExportType exportType,
        string fileName,
        long fileSize,
        object? parameters = null,
        string? filePath = null)
    {
        var export = new ExportHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ExportType = exportType,
            FileName = fileName,
            FileSize = fileSize,
            Parameters = parameters != null ? JsonSerializer.Serialize(parameters) : null,
            FilePath = filePath,
            DownloadCount = 0,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30), // Default 30-day expiration
            IsDeleted = false
        };

        _context.Add(export);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Export logged: {ExportType} by user {UserId}, file {FileName} ({FileSize} bytes)",
            exportType, userId, fileName, fileSize);

        return export.Id;
    }

    public async Task<List<ExportHistoryDto>> GetUserExportsAsync(
        Guid userId,
        int limit = 20,
        ExportType? exportType = null)
    {
        var query = _context.Set<ExportHistory>()
            .Include(e => e.User)
            .Where(e => e.UserId == userId && !e.IsDeleted);

        if (exportType.HasValue)
        {
            query = query.Where(e => e.ExportType == exportType.Value);
        }

        var exports = await query
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return exports.Select(MapToDto).ToList();
    }

    public async Task<List<ExportHistoryDto>> GetAllExportsAsync(
        int limit = 100,
        ExportType? exportType = null)
    {
        var query = _context.Set<ExportHistory>()
            .Include(e => e.User)
            .Where(e => !e.IsDeleted);

        if (exportType.HasValue)
        {
            query = query.Where(e => e.ExportType == exportType.Value);
        }

        var exports = await query
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return exports.Select(MapToDto).ToList();
    }

    public async Task<ExportHistoryDto?> GetExportAsync(Guid exportId)
    {
        var export = await _context.Set<ExportHistory>()
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == exportId && !e.IsDeleted);

        return export != null ? MapToDto(export) : null;
    }

    public async Task IncrementDownloadCountAsync(Guid exportId)
    {
        var export = await _context.Set<ExportHistory>()
            .FirstOrDefaultAsync(e => e.Id == exportId);

        if (export != null)
        {
            export.DownloadCount++;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Download count incremented for export {ExportId}, new count: {DownloadCount}",
                exportId, export.DownloadCount);
        }
    }

    public async Task DeleteExportAsync(Guid exportId)
    {
        var export = await _context.Set<ExportHistory>()
            .FirstOrDefaultAsync(e => e.Id == exportId);

        if (export != null)
        {
            // Soft delete
            export.IsDeleted = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Export {ExportId} soft deleted", exportId);

            // TODO: If file is stored on disk, delete the physical file
            if (!string.IsNullOrEmpty(export.FilePath) && File.Exists(export.FilePath))
            {
                try
                {
                    File.Delete(export.FilePath);
                    _logger.LogInformation("Physical file deleted: {FilePath}", export.FilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete physical file: {FilePath}", export.FilePath);
                }
            }
        }
    }

    public async Task<int> CleanupExpiredExportsAsync(int daysOld = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);

        var expiredExports = await _context.Set<ExportHistory>()
            .Where(e => !e.IsDeleted && e.CreatedAt < cutoffDate)
            .ToListAsync();

        foreach (var export in expiredExports)
        {
            export.IsDeleted = true;

            // Delete physical file if exists
            if (!string.IsNullOrEmpty(export.FilePath) && File.Exists(export.FilePath))
            {
                try
                {
                    File.Delete(export.FilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to delete physical file during cleanup: {FilePath}",
                        export.FilePath);
                }
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Cleaned up {Count} expired exports older than {Days} days",
            expiredExports.Count, daysOld);

        return expiredExports.Count;
    }

    public async Task<ExportStatsDto> GetExportStatsAsync(Guid? userId = null, Guid? facultyId = null)
    {
        var query = _context.Set<ExportHistory>()
            .Where(e => !e.IsDeleted);

        if (userId.HasValue)
        {
            query = query.Where(e => e.UserId == userId.Value);
        }

        // TODO: Add faculty filtering when needed
        // This would require joining with User -> Student/Professor -> Faculty
        // For now, we'll leave it as a simple user-based filter

        var exports = await query.ToListAsync();

        var stats = new ExportStatsDto
        {
            TotalExports = exports.Count,
            TotalDownloads = exports.Sum(e => e.DownloadCount),
            TotalFileSize = exports.Sum(e => e.FileSize),
            TotalFileSizeFormatted = FormatFileSize(exports.Sum(e => e.FileSize)),
            MostRecentExport = exports.Any() ? exports.Max(e => e.CreatedAt) : null,
            ExportsByType = exports
                .GroupBy(e => e.ExportType)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        // Find most popular export
        var mostPopular = exports
            .GroupBy(e => e.ExportType)
            .Select(g => new ExportTypeStatsDto
            {
                Type = g.Key,
                TypeName = GetExportTypeName(g.Key),
                Count = g.Count(),
                TotalDownloads = g.Sum(e => e.DownloadCount)
            })
            .OrderByDescending(s => s.TotalDownloads)
            .FirstOrDefault();

        stats.MostPopularExport = mostPopular;

        return stats;
    }

    // ========================================
    // Helper Methods
    // ========================================

    private ExportHistoryDto MapToDto(ExportHistory export)
    {
        return new ExportHistoryDto
        {
            Id = export.Id,
            UserId = export.UserId,
            UserName = $"{export.User.FirstName} {export.User.LastName}",
            ExportType = export.ExportType,
            ExportTypeName = GetExportTypeName(export.ExportType),
            FileName = export.FileName,
            Parameters = export.Parameters,
            FileSizeBytes = export.FileSize,
            FileSizeFormatted = FormatFileSize(export.FileSize),
            DownloadCount = export.DownloadCount,
            CreatedAt = export.CreatedAt,
            CreatedAtRelative = GetRelativeTime(export.CreatedAt),
            ExpiresAt = export.ExpiresAt,
            IsExpired = export.ExpiresAt.HasValue && export.ExpiresAt.Value < DateTime.UtcNow,
            FilePath = export.FilePath
        };
    }

    private static string GetExportTypeName(ExportType type)
    {
        return type switch
        {
            ExportType.SituatieScolara => "Situație Școlară (PDF)",
            ExportType.AdeverintaStudent => "Adeverință Student (PDF)",
            ExportType.CatalogNote => "Catalog Note (PDF)",
            ExportType.RaportFacultate => "Raport Facultate (PDF)",
            ExportType.StudentsExcel => "Lista Studenți (Excel)",
            ExportType.GradesExcel => "Lista Note (Excel)",
            ExportType.AttendanceExcel => "Lista Prezență (Excel)",
            ExportType.ActivityLogExcel => "Jurnal Activitate (Excel)",
            _ => type.ToString()
        };
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    private static string GetRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        if (timeSpan.TotalMinutes < 1)
            return "acum";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} min";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} ore";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays} zile";
        if (timeSpan.TotalDays < 30)
            return $"{(int)(timeSpan.TotalDays / 7)} săptămâni";
        if (timeSpan.TotalDays < 365)
            return $"{(int)(timeSpan.TotalDays / 30)} luni";

        return $"{(int)(timeSpan.TotalDays / 365)} ani";
    }
}
