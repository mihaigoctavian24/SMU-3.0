using SMU.Data.Entities;

namespace SMU.Services;

/// <summary>
/// Service for generating PDF documents for student document requests
/// </summary>
public interface IDocumentGenerationService
{
    /// <summary>
    /// Generates a Student Certificate PDF
    /// </summary>
    Task<byte[]> GenerateStudentCertificateAsync(Guid studentId);

    /// <summary>
    /// Generates a Grade Report PDF with all grades
    /// </summary>
    Task<byte[]> GenerateGradeReportAsync(Guid studentId);

    /// <summary>
    /// Generates an Enrollment Proof PDF (Foaie Matricolă)
    /// </summary>
    Task<byte[]> GenerateEnrollmentProofAsync(Guid studentId);

    /// <summary>
    /// Saves PDF bytes to wwwroot/documents and returns the file path
    /// </summary>
    Task<string> SaveDocumentAsync(byte[] pdfBytes, RequestType type, string studentNumber);
}
