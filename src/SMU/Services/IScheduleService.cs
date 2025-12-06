using SMU.Services.DTOs;

namespace SMU.Services;

/// <summary>
/// Service interface for schedule management
/// Handles CRUD operations for schedule entries and weekly schedule views
/// </summary>
public interface IScheduleService
{
    /// <summary>
    /// Get schedule entries by group with optional date filtering
    /// </summary>
    Task<List<ScheduleEntryDto>> GetByGroupAsync(Guid groupId, DateOnly? startDate = null, DateOnly? endDate = null);

    /// <summary>
    /// Get schedule entries by professor with optional date filtering
    /// </summary>
    Task<List<ScheduleEntryDto>> GetByProfessorAsync(Guid professorId, DateOnly? startDate = null, DateOnly? endDate = null);

    /// <summary>
    /// Get schedule entries for a student (via their group) with optional date filtering
    /// </summary>
    Task<List<ScheduleEntryDto>> GetByStudentAsync(Guid studentId, DateOnly? startDate = null, DateOnly? endDate = null);

    /// <summary>
    /// Get a single schedule entry by ID
    /// </summary>
    Task<ScheduleEntryDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get weekly schedule view by group
    /// </summary>
    Task<List<WeeklyScheduleDto>> GetWeeklyScheduleByGroupAsync(Guid groupId);

    /// <summary>
    /// Get weekly schedule view by professor
    /// </summary>
    Task<List<WeeklyScheduleDto>> GetWeeklyScheduleByProfessorAsync(Guid professorId);

    /// <summary>
    /// Get weekly schedule view by student
    /// </summary>
    Task<List<WeeklyScheduleDto>> GetWeeklyScheduleByStudentAsync(Guid studentId);

    /// <summary>
    /// Create a new schedule entry
    /// </summary>
    Task<ServiceResult<Guid>> CreateAsync(CreateScheduleEntryDto dto);

    /// <summary>
    /// Update an existing schedule entry
    /// </summary>
    Task<ServiceResult> UpdateAsync(Guid id, UpdateScheduleEntryDto dto);

    /// <summary>
    /// Delete a schedule entry
    /// </summary>
    Task<ServiceResult> DeleteAsync(Guid id);

    /// <summary>
    /// Create multiple schedule entries at once
    /// </summary>
    Task<ServiceResult> BulkCreateAsync(List<CreateScheduleEntryDto> entries);

    /// <summary>
    /// Check for schedule conflicts (same room, same time)
    /// </summary>
    Task<ScheduleConflictDto> CheckConflictAsync(Guid? excludeId, int dayOfWeek, TimeOnly startTime, TimeOnly endTime, string? room);

    /// <summary>
    /// Get all groups for schedule management
    /// </summary>
    Task<List<GroupOptionDto>> GetGroupOptionsAsync();

    /// <summary>
    /// Get all courses for schedule management
    /// </summary>
    Task<List<CourseOptionDto>> GetCourseOptionsAsync();

    /// <summary>
    /// Get today's schedule for a student
    /// </summary>
    Task<List<ScheduleEntryDto>> GetTodayScheduleForStudentAsync(Guid studentId);

    /// <summary>
    /// Get today's schedule for a professor
    /// </summary>
    Task<List<ScheduleEntryDto>> GetTodayScheduleForProfessorAsync(Guid professorId);

    /// <summary>
    /// Get weekly schedule by faculty with optional year and semester filtering (for Dean role)
    /// </summary>
    Task<List<WeeklyScheduleDto>> GetWeeklyScheduleByFacultyAsync(Guid facultyId, int? year = null, int? semester = null);

    /// <summary>
    /// Get available years for a faculty's groups
    /// </summary>
    Task<List<int>> GetAvailableYearsAsync(Guid facultyId);

    /// <summary>
    /// Get groups by faculty with optional year filter (for Dean filtering)
    /// </summary>
    Task<List<GroupOptionDto>> GetGroupsByFacultyAsync(Guid facultyId, int? year = null);
}
