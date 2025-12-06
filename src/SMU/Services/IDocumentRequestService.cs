using SMU.Services.DTOs;

namespace SMU.Services;

/// <summary>
/// Interface for document request management operations
/// </summary>
public interface IDocumentRequestService
{
    /// <summary>
    /// Get all document requests for a specific student
    /// </summary>
    Task<List<DocumentRequestDto>> GetByStudentAsync(Guid studentId);

    /// <summary>
    /// Get pending document requests, optionally filtered by faculty
    /// </summary>
    Task<List<DocumentRequestDto>> GetPendingAsync(Guid? facultyId = null);

    /// <summary>
    /// Get all document requests with optional filters
    /// </summary>
    Task<List<DocumentRequestDto>> GetAllAsync(DocumentRequestFilter filter);

    /// <summary>
    /// Get detailed information about a specific document request
    /// </summary>
    Task<DocumentRequestDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Create a new document request (student)
    /// </summary>
    Task<ServiceResult<Guid>> CreateAsync(CreateDocumentRequestDto dto, Guid studentId);

    /// <summary>
    /// Process a document request (approve/reject by secretary)
    /// </summary>
    Task<ServiceResult> ProcessAsync(Guid id, ProcessDocumentRequestDto dto, Guid processedById);

    /// <summary>
    /// Complete a document request by emitting the document (secretary)
    /// </summary>
    Task<ServiceResult> CompleteAsync(Guid id, CompleteDocumentRequestDto dto, Guid completedById);

    /// <summary>
    /// Cancel a document request (student can cancel own pending requests)
    /// </summary>
    Task<ServiceResult> CancelAsync(Guid id, Guid studentId);
}

// ServiceResult classes are defined in CourseService.cs
