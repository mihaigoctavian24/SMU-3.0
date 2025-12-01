namespace SMU.Services.DTOs;

/// <summary>
/// DTO for listing schedule entries
/// </summary>
public class ScheduleEntryDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public Guid? ProfessorId { get; set; }
    public string ProfessorName { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }
    public string DayName { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Room { get; set; }
    public string Type { get; set; } = "Curs";
    public string TypeLabel { get; set; } = "Curs";
}

/// <summary>
/// DTO for creating a new schedule entry
/// </summary>
public class CreateScheduleEntryDto
{
    public Guid CourseId { get; set; }
    public Guid GroupId { get; set; }
    public int DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Room { get; set; }
    public string Type { get; set; } = "Curs";
}

/// <summary>
/// DTO for updating an existing schedule entry
/// </summary>
public class UpdateScheduleEntryDto
{
    public int DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Room { get; set; }
    public string Type { get; set; } = "Curs";
}

/// <summary>
/// DTO for weekly schedule view
/// </summary>
public class WeeklyScheduleDto
{
    public int DayOfWeek { get; set; }
    public string DayName { get; set; } = string.Empty;
    public List<ScheduleEntryDto> Entries { get; set; } = new();
}

/// <summary>
/// DTO for group selection in schedule management
/// </summary>
public class GroupOptionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ProgramName { get; set; } = string.Empty;
    public int Year { get; set; }
}

/// <summary>
/// DTO for course selection in schedule management
/// </summary>
public class CourseOptionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ProfessorName { get; set; } = string.Empty;
}

/// <summary>
/// DTO for schedule conflict detection
/// </summary>
public class ScheduleConflictDto
{
    public bool HasConflict { get; set; }
    public string ConflictMessage { get; set; } = string.Empty;
    public ScheduleEntryDto? ConflictingEntry { get; set; }
}
