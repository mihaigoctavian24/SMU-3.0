namespace SMU.Data.Entities;

/// <summary>
/// Core domain entities matching Supabase schema
/// </summary>

public class Faculty
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? DeanId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Professor? Dean { get; set; }
    public ICollection<Program> Programs { get; set; } = new List<Program>();
    public ICollection<Professor> Professors { get; set; } = new List<Professor>();
}

public class Program
{
    public Guid Id { get; set; }
    public Guid FacultyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public ProgramType Type { get; set; }
    public int DurationYears { get; set; }
    public int TotalCredits { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Faculty Faculty { get; set; } = null!;
    public ICollection<Group> Groups { get; set; } = new List<Group>();
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}

public class Group
{
    public Guid Id { get; set; }
    public Guid ProgramId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public int MaxStudents { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Program Program { get; set; } = null!;
    public ICollection<Student> Students { get; set; } = new List<Student>();
    public ICollection<ScheduleEntry> ScheduleEntries { get; set; } = new List<ScheduleEntry>();
}

public class Student
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid GroupId { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    public int CurrentYear { get; set; }
    public StudentStatus Status { get; set; } = StudentStatus.Active;
    public DateTime EnrollmentDate { get; set; }
    public DateTime? GraduationDate { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal Gpa { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Group Group { get; set; } = null!;
    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<DocumentRequest> DocumentRequests { get; set; } = new List<DocumentRequest>();
}

public class Professor
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid FacultyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Office { get; set; }
    public string? Phone { get; set; }
    public DateTime HireDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Faculty Faculty { get; set; } = null!;
    public ICollection<Course> Courses { get; set; } = new List<Course>();
    public ICollection<Grade> GradesGiven { get; set; } = new List<Grade>();
}

public class Course
{
    public Guid Id { get; set; }
    public Guid ProgramId { get; set; }
    public Guid ProfessorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Credits { get; set; }
    public int Semester { get; set; }
    public int Year { get; set; }
    public string? Description { get; set; }
    public string? Syllabus { get; set; }
    public bool IsOptional { get; set; }
    public int MaxStudents { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Program Program { get; set; } = null!;
    public Professor Professor { get; set; } = null!;
    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<ScheduleEntry> ScheduleEntries { get; set; } = new List<ScheduleEntry>();
}

public class Grade
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public Guid ProfessorId { get; set; }
    public decimal Value { get; set; }
    public GradeType Type { get; set; }
    public GradeStatus Status { get; set; } = GradeStatus.Pending;
    public DateTime GradedAt { get; set; }
    public string? Comments { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
    public Professor Professor { get; set; } = null!;
}

public class Attendance
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public DateOnly Date { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Notes { get; set; }
    public Guid? MarkedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
    public Professor? MarkedBy { get; set; }
}

public class ScheduleEntry
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid GroupId { get; set; }
    public int DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Room { get; set; }
    public string? Building { get; set; }
    public GradeType Type { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Course Course { get; set; } = null!;
    public Group Group { get; set; } = null!;
}

public class DocumentRequest
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public RequestType Type { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public string? Description { get; set; }
    public string? Response { get; set; }
    public Guid? ProcessedById { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Student Student { get; set; } = null!;
    public ApplicationUser? ProcessedBy { get; set; }
}

public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public bool IsRead { get; set; }
    public string? Link { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
}

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ApplicationUser? User { get; set; }
}
