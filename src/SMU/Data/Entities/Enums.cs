namespace SMU.Data.Entities;

/// <summary>
/// PostgreSQL ENUMs matching Supabase schema
/// All values must match exactly (PascalCase)
/// </summary>

public enum UserRole
{
    Student,
    Professor,
    Secretary,
    Dean,
    Rector,
    Admin
}

public enum ProgramType
{
    Bachelor,
    Master,
    PhD
}

public enum StudentStatus
{
    Active,
    Inactive,
    Graduated,
    Expelled,
    Suspended
}

public enum GradeType
{
    Exam,
    Lab,
    Seminar,
    Project,
    Final
}

public enum GradeStatus
{
    Pending,
    Approved,
    Rejected
}

public enum AttendanceStatus
{
    Present,
    Absent,
    Excused
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error,
    GradeAdded,
    AttendanceMarked,
    RequestUpdate
}

public enum RequestStatus
{
    Pending,
    InProgress,
    Approved,
    Rejected,
    Completed
}

public enum RequestType
{
    StudentCertificate,
    GradeReport,
    EnrollmentProof,
    Other
}

public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}
