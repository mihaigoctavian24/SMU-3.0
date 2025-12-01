namespace SMU.Services;

/// <summary>
/// Service interface for exporting data to PDF and Excel formats
/// </summary>
public interface IExportService
{
    // ========================================
    // PDF Exports
    // ========================================

    /// <summary>
    /// Export academic transcript (Situație Școlară) for a student
    /// Includes all grades, credits, average, and status
    /// </summary>
    /// <param name="studentId">Student ID</param>
    /// <returns>PDF file as byte array</returns>
    Task<byte[]> ExportSituatieScolara(Guid studentId);

    /// <summary>
    /// Export student certificate (Adeverință Student)
    /// Formal document certifying student enrollment
    /// </summary>
    /// <param name="studentId">Student ID</param>
    /// <param name="purpose">Purpose of the certificate (e.g., "pentru bursă", "pentru bancă")</param>
    /// <returns>PDF file as byte array</returns>
    Task<byte[]> ExportAdeverintaStudent(Guid studentId, string purpose);

    /// <summary>
    /// Export grade catalog (Catalog Note) for a course
    /// Includes all students in the course with their grades
    /// </summary>
    /// <param name="courseId">Course ID</param>
    /// <returns>PDF file as byte array</returns>
    Task<byte[]> ExportCatalogNote(Guid courseId);

    /// <summary>
    /// Export faculty report (Raport Facultate)
    /// Includes statistics per program: student count, average, pass rate
    /// </summary>
    /// <param name="facultyId">Faculty ID</param>
    /// <returns>PDF file as byte array</returns>
    Task<byte[]> ExportRaportFacultate(Guid facultyId);

    // ========================================
    // Excel Exports
    // ========================================

    /// <summary>
    /// Export students list to Excel
    /// Optional filtering by faculty or program
    /// </summary>
    /// <param name="facultyId">Optional faculty filter</param>
    /// <param name="programId">Optional program filter</param>
    /// <returns>Excel file as byte array</returns>
    Task<byte[]> ExportStudentsToExcel(Guid? facultyId = null, Guid? programId = null);

    /// <summary>
    /// Export grades to Excel
    /// Optional filtering by course or student
    /// </summary>
    /// <param name="courseId">Optional course filter</param>
    /// <param name="studentId">Optional student filter</param>
    /// <returns>Excel file as byte array</returns>
    Task<byte[]> ExportGradesToExcel(Guid? courseId = null, Guid? studentId = null);

    /// <summary>
    /// Export attendance records to Excel
    /// Optional filtering by course or student
    /// </summary>
    /// <param name="courseId">Optional course filter</param>
    /// <param name="studentId">Optional student filter</param>
    /// <returns>Excel file as byte array</returns>
    Task<byte[]> ExportAttendanceToExcel(Guid? courseId = null, Guid? studentId = null);
}
