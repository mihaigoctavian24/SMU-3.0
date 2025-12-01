namespace SMU.Models;

/// <summary>
/// DTOs for Course operations
/// </summary>

public class CourseFilter
{
    public Guid? ProgramId { get; set; }
    public Guid? ProfessorId { get; set; }
    public int? Year { get; set; }
    public int? Semester { get; set; }
    public bool? IsActive { get; set; }
    public string? SearchTerm { get; set; }
}

public class CreateCourseDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Guid ProgramId { get; set; }
    public Guid? ProfessorId { get; set; }
    public int Credits { get; set; } = 5;
    public int Year { get; set; }
    public int Semester { get; set; }
    public string? Description { get; set; }
}

public class UpdateCourseDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Guid? ProfessorId { get; set; }
    public int Credits { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CourseListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ProfessorName { get; set; } = "Neasignat";
    public string ProgramName { get; set; } = string.Empty;
    public int Credits { get; set; }
    public int Year { get; set; }
    public int Semester { get; set; }
    public int StudentsCount { get; set; }
    public bool IsActive { get; set; }
}

public class CourseDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ProfessorName { get; set; } = "Neasignat";
    public Guid? ProfessorId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public Guid ProgramId { get; set; }
    public string FacultyName { get; set; } = string.Empty;
    public int Credits { get; set; }
    public int Year { get; set; }
    public int Semester { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public CourseStatsDto Stats { get; set; } = new();
    public List<RecentGradeDto> RecentGrades { get; set; } = new();
}

public class CourseStatsDto
{
    public int EnrolledStudents { get; set; }
    public decimal AverageGrade { get; set; }
    public decimal PassRate { get; set; }
    public int GradesCount { get; set; }
    public int TotalClasses { get; set; }
}

public class RecentGradeDto
{
    public string StudentName { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateOnly ExamDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ProgramOption
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class ProfessorOption
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Title { get; set; }
}
