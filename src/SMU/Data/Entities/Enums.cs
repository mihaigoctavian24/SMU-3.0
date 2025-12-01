using NpgsqlTypes;

namespace SMU.Data.Entities;

/// <summary>
/// PostgreSQL ENUMs matching Supabase schema
/// All values must match exactly (PascalCase)
/// PgName attributes map C# enums to PostgreSQL enum types
/// </summary>

[PgName("user_role")]
public enum UserRole
{
    [PgName("Student")] Student,
    [PgName("Professor")] Professor,
    [PgName("Secretary")] Secretary,
    [PgName("Dean")] Dean,
    [PgName("Rector")] Rector,
    [PgName("Admin")] Admin
}

[PgName("program_type")]
public enum ProgramType
{
    [PgName("Bachelor")] Bachelor,
    [PgName("Master")] Master,
    [PgName("PhD")] PhD
}

[PgName("student_status")]
public enum StudentStatus
{
    [PgName("Active")] Active,
    [PgName("Inactive")] Inactive,
    [PgName("Graduated")] Graduated,
    [PgName("Expelled")] Expelled,
    [PgName("Suspended")] Suspended
}

[PgName("grade_type")]
public enum GradeType
{
    [PgName("Exam")] Exam,
    [PgName("Lab")] Lab,
    [PgName("Seminar")] Seminar,
    [PgName("Project")] Project,
    [PgName("Final")] Final
}

[PgName("grade_status")]
public enum GradeStatus
{
    [PgName("Pending")] Pending,
    [PgName("Approved")] Approved,
    [PgName("Rejected")] Rejected
}

[PgName("attendance_status")]
public enum AttendanceStatus
{
    [PgName("Present")] Present,
    [PgName("Absent")] Absent,
    [PgName("Excused")] Excused
}

[PgName("notification_type")]
public enum NotificationType
{
    [PgName("Info")] Info,
    [PgName("Success")] Success,
    [PgName("Warning")] Warning,
    [PgName("Error")] Error,
    [PgName("GradeAdded")] GradeAdded,
    [PgName("AttendanceMarked")] AttendanceMarked,
    [PgName("RequestUpdate")] RequestUpdate
}

[PgName("request_status")]
public enum RequestStatus
{
    [PgName("Pending")] Pending,
    [PgName("InProgress")] InProgress,
    [PgName("Approved")] Approved,
    [PgName("Rejected")] Rejected,
    [PgName("Completed")] Completed
}

[PgName("request_type")]
public enum RequestType
{
    [PgName("StudentCertificate")] StudentCertificate,
    [PgName("GradeReport")] GradeReport,
    [PgName("EnrollmentProof")] EnrollmentProof,
    [PgName("Other")] Other
}

[PgName("risk_level")]
public enum RiskLevel
{
    [PgName("Low")] Low,
    [PgName("Medium")] Medium,
    [PgName("High")] High,
    [PgName("Critical")] Critical
}

[PgName("export_type")]
public enum ExportType
{
    [PgName("SituatieScolara")] SituatieScolara,           // Academic transcript PDF
    [PgName("AdeverintaStudent")] AdeverintaStudent,       // Student certificate PDF
    [PgName("CatalogNote")] CatalogNote,                   // Course grades PDF
    [PgName("RaportFacultate")] RaportFacultate,           // Faculty report PDF
    [PgName("StudentsExcel")] StudentsExcel,               // Students list Excel
    [PgName("GradesExcel")] GradesExcel,                   // Grades list Excel
    [PgName("AttendanceExcel")] AttendanceExcel,           // Attendance list Excel
    [PgName("ActivityLogExcel")] ActivityLogExcel          // Activity log Excel (future)
}

/// <summary>
/// Activity action types for audit logging
/// (Not a PostgreSQL enum - used for filtering and display logic)
/// </summary>
public enum ActivityAction
{
    Created,
    Updated,
    Deleted,
    Viewed,
    Exported,
    LoggedIn,
    LoggedOut,
    Approved,
    Rejected,
    Assigned
}
