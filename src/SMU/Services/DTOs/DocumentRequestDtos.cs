using SMU.Data.Entities;

namespace SMU.Services.DTOs;

/// <summary>
/// DTO for document request display in list/detail views
/// </summary>
public class DocumentRequestDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentNumber { get; set; } = string.Empty;
    public RequestType Type { get; set; }
    public string TypeLabel { get; set; } = string.Empty;
    public RequestStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public Guid? ProcessedById { get; set; }
    public string? ProcessedByName { get; set; }
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Extracts the generated document path from Notes field
    /// </summary>
    public string? GeneratedDocumentPath
    {
        get
        {
            if (string.IsNullOrEmpty(Notes) || Status != RequestStatus.Completed)
                return null;

            var prefix = "Document generat: ";
            var index = Notes.IndexOf(prefix);
            if (index == -1)
                return null;

            var pathStart = index + prefix.Length;
            var lineEnd = Notes.IndexOf('\n', pathStart);

            return lineEnd > pathStart
                ? Notes.Substring(pathStart, lineEnd - pathStart).Trim()
                : Notes.Substring(pathStart).Trim();
        }
    }
}

/// <summary>
/// DTO for creating a new document request
/// </summary>
public class CreateDocumentRequestDto
{
    public RequestType Type { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for processing (approve/reject) a document request
/// </summary>
public class ProcessDocumentRequestDto
{
    public bool Approved { get; set; }
    public string? RejectionReason { get; set; }
}

/// <summary>
/// DTO for completing/emitting a document request
/// </summary>
public class CompleteDocumentRequestDto
{
    public string? GeneratedDocumentPath { get; set; }
}

/// <summary>
/// Filter criteria for document requests
/// </summary>
public class DocumentRequestFilter
{
    public Guid? StudentId { get; set; }
    public Guid? FacultyId { get; set; }
    public RequestStatus? Status { get; set; }
    public RequestType? Type { get; set; }
}
