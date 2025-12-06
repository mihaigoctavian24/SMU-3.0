using Microsoft.EntityFrameworkCore;
using SMU.Data;
using SMU.Data.Entities;
using SMU.Services.DTOs;

namespace SMU.Services;

/// <summary>
/// Service for managing document request operations
/// </summary>
public class DocumentRequestService : IDocumentRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DocumentRequestService> _logger;
    private readonly INotificationService _notificationService;
    private readonly IDocumentGenerationService _documentGenerationService;

    public DocumentRequestService(
        ApplicationDbContext context,
        ILogger<DocumentRequestService> logger,
        INotificationService notificationService,
        IDocumentGenerationService documentGenerationService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
        _documentGenerationService = documentGenerationService;
    }

    public async Task<List<DocumentRequestDto>> GetByStudentAsync(Guid studentId)
    {
        return await _context.DocumentRequests
            .Where(r => r.StudentId == studentId)
            .Include(r => r.Student)
                .ThenInclude(s => s.User)
            .Include(r => r.ProcessedBy)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => MapToDto(r))
            .ToListAsync();
    }

    public async Task<List<DocumentRequestDto>> GetPendingAsync(Guid? facultyId = null)
    {
        var query = _context.DocumentRequests
            .Where(r => r.Status == RequestStatus.Pending)
            .Include(r => r.Student)
                .ThenInclude(s => s.User)
            .Include(r => r.Student)
                .ThenInclude(s => s.Group)
                    .ThenInclude(g => g.Program)
                        .ThenInclude(p => p.Faculty)
            .Include(r => r.ProcessedBy)
            .AsQueryable();

        if (facultyId.HasValue)
        {
            query = query.Where(r => r.Student.Group != null &&
                                    r.Student.Group.Program.FacultyId == facultyId.Value);
        }

        return await query
            .OrderBy(r => r.CreatedAt)
            .Select(r => MapToDto(r))
            .ToListAsync();
    }

    public async Task<List<DocumentRequestDto>> GetAllAsync(DocumentRequestFilter filter)
    {
        var query = _context.DocumentRequests
            .Include(r => r.Student)
                .ThenInclude(s => s.User)
            .Include(r => r.Student)
                .ThenInclude(s => s.Group)
                    .ThenInclude(g => g.Program)
                        .ThenInclude(p => p.Faculty)
            .Include(r => r.ProcessedBy)
            .AsQueryable();

        if (filter.StudentId.HasValue)
        {
            query = query.Where(r => r.StudentId == filter.StudentId.Value);
        }

        if (filter.FacultyId.HasValue)
        {
            query = query.Where(r => r.Student.Group != null &&
                                    r.Student.Group.Program.FacultyId == filter.FacultyId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(r => r.Status == filter.Status.Value);
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(r => r.Type == filter.Type.Value);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => MapToDto(r))
            .ToListAsync();
    }

    public async Task<DocumentRequestDto?> GetByIdAsync(Guid id)
    {
        var request = await _context.DocumentRequests
            .Where(r => r.Id == id)
            .Include(r => r.Student)
                .ThenInclude(s => s.User)
            .Include(r => r.ProcessedBy)
            .FirstOrDefaultAsync();

        return request != null ? MapToDto(request) : null;
    }

    public async Task<ServiceResult<Guid>> CreateAsync(CreateDocumentRequestDto dto, Guid studentId)
    {
        // Validate student exists
        var student = await _context.Students
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == studentId);

        if (student == null)
        {
            _logger.LogWarning("Attempted to create request for non-existent student: {StudentId}", studentId);
            return ServiceResult<Guid>.Failed("Studentul nu există.");
        }

        // Check for pending duplicate requests of same type
        var hasPendingDuplicate = await _context.DocumentRequests
            .AnyAsync(r => r.StudentId == studentId &&
                          r.Type == dto.Type &&
                          r.Status == RequestStatus.Pending);

        if (hasPendingDuplicate)
        {
            return ServiceResult<Guid>.Failed($"Există deja o cerere în așteptare de tip {GetTypeLabel(dto.Type)}.");
        }

        var request = new DocumentRequest
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            Type = dto.Type,
            Status = RequestStatus.Pending,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.DocumentRequests.Add(request);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Document request created: {RequestId} by student {StudentId}", request.Id, studentId);
        return ServiceResult<Guid>.Success(request.Id);
    }

    public async Task<ServiceResult> ProcessAsync(Guid id, ProcessDocumentRequestDto dto, Guid processedById)
    {
        var request = await _context.DocumentRequests
            .Include(r => r.Student)
                .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
        {
            return ServiceResult.Failed("Cererea nu a fost găsită.");
        }

        if (request.Status != RequestStatus.Pending)
        {
            return ServiceResult.Failed("Doar cererile cu status Pending pot fi procesate.");
        }

        request.Status = dto.Approved ? RequestStatus.Approved : RequestStatus.Rejected;
        request.ProcessedById = processedById;
        request.ProcessedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        if (!dto.Approved && !string.IsNullOrWhiteSpace(dto.RejectionReason))
        {
            request.Notes = string.IsNullOrEmpty(request.Notes)
                ? $"Respins: {dto.RejectionReason}"
                : $"{request.Notes}\nRespins: {dto.RejectionReason}";
        }

        await _context.SaveChangesAsync();

        var action = dto.Approved ? "aprobată" : "respinsă";
        _logger.LogInformation("Document request {Action}: {RequestId} by user {UserId}",
            action, id, processedById);

        // Send notification to student
        var studentUserId = request.Student.User.Id;
        if (dto.Approved)
        {
            await _notificationService.SendAsync(
                studentUserId,
                "Cerere Aprobată",
                $"Cererea ta pentru {GetTypeLabel(request.Type)} a fost aprobată și este în curs de procesare.",
                NotificationType.RequestUpdate,
                "/cereri"
            );
        }
        else
        {
            var reason = !string.IsNullOrWhiteSpace(dto.RejectionReason)
                ? $" Motiv: {dto.RejectionReason}"
                : "";
            await _notificationService.SendAsync(
                studentUserId,
                "Cerere Respinsă",
                $"Cererea ta pentru {GetTypeLabel(request.Type)} a fost respinsă.{reason}",
                NotificationType.Warning,
                "/cereri"
            );
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> CompleteAsync(Guid id, CompleteDocumentRequestDto dto, Guid completedById)
    {
        var request = await _context.DocumentRequests
            .Include(r => r.Student)
                .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
        {
            return ServiceResult.Failed("Cererea nu a fost găsită.");
        }

        if (request.Status != RequestStatus.Approved)
        {
            return ServiceResult.Failed("Doar cererile aprobate pot fi finalizate.");
        }

        try
        {
            // Generate PDF based on request type
            byte[] pdfBytes = request.Type switch
            {
                RequestType.StudentCertificate => await _documentGenerationService.GenerateStudentCertificateAsync(request.StudentId),
                RequestType.GradeReport => await _documentGenerationService.GenerateGradeReportAsync(request.StudentId),
                RequestType.EnrollmentProof => await _documentGenerationService.GenerateEnrollmentProofAsync(request.StudentId),
                _ => throw new InvalidOperationException($"Tip document necunoscut: {request.Type}")
            };

            // Save PDF and get file path
            var documentPath = await _documentGenerationService.SaveDocumentAsync(
                pdfBytes,
                request.Type,
                request.Student.StudentNumber
            );

            // Mark as completed
            request.Status = RequestStatus.Completed;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedById = completedById;
            request.UpdatedAt = DateTime.UtcNow;

            // Store the generated document path
            request.Notes = string.IsNullOrEmpty(request.Notes)
                ? $"Document generat: {documentPath}"
                : $"{request.Notes}\nDocument generat: {documentPath}";

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Eroare la generarea documentului pentru cererea {RequestId}", id);
            return ServiceResult.Failed($"Eroare la generarea documentului: {ex.Message}");
        }

        var action = "completed";
        _logger.LogInformation(
            "Document request {Action}: {RequestId} by user {UserId}",
            action, id, completedById);

        // Send notification to student about document completion
        var student = await _context.Students
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == request.StudentId);

        if (student != null)
        {
            await _notificationService.SendAsync(
                student.User.Id,
                "Document Emis",
                $"Documentul tău ({GetTypeLabel(request.Type)}) a fost emis și este gata de ridicare.",
                NotificationType.Success,
                "/cereri"
            );
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> CancelAsync(Guid id, Guid studentId)
    {
        var request = await _context.DocumentRequests
            .FirstOrDefaultAsync(r => r.Id == id && r.StudentId == studentId);

        if (request == null)
        {
            return ServiceResult.Failed("Cererea nu a fost găsită sau nu vă aparține.");
        }

        if (request.Status != RequestStatus.Pending)
        {
            return ServiceResult.Failed("Doar cererile în așteptare pot fi anulate.");
        }

        request.Status = RequestStatus.Rejected;
        request.Notes = string.IsNullOrEmpty(request.Notes)
            ? "Anulată de student"
            : $"{request.Notes}\nAnulată de student";
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Document request cancelled: {RequestId} by student {StudentId}", id, studentId);
        return ServiceResult.Success();
    }

    // Helper method to map entity to DTO
    private static DocumentRequestDto MapToDto(DocumentRequest request)
    {
        return new DocumentRequestDto
        {
            Id = request.Id,
            StudentId = request.StudentId,
            StudentName = $"{request.Student.User.FirstName} {request.Student.User.LastName}",
            StudentNumber = request.Student.StudentNumber,
            Type = request.Type,
            TypeLabel = GetTypeLabel(request.Type),
            Status = request.Status,
            StatusLabel = GetStatusLabel(request.Status),
            Notes = request.Notes,
            CreatedAt = request.CreatedAt,
            ProcessedAt = request.ProcessedAt,
            ProcessedById = request.ProcessedById,
            ProcessedByName = request.ProcessedBy != null
                ? $"{request.ProcessedBy.FirstName} {request.ProcessedBy.LastName}"
                : null,
            RejectionReason = request.Status == RequestStatus.Rejected && request.Notes != null
                ? ExtractRejectionReason(request.Notes)
                : null
        };
    }

    private static string GetTypeLabel(RequestType type)
    {
        return type switch
        {
            RequestType.StudentCertificate => "Adeverință de Student",
            RequestType.GradeReport => "Adeverință cu Note",
            RequestType.EnrollmentProof => "Foaie Matricolă",
            RequestType.Other => "Altele",
            _ => type.ToString()
        };
    }

    private static string GetStatusLabel(RequestStatus status)
    {
        return status switch
        {
            RequestStatus.Pending => "În așteptare",
            RequestStatus.InProgress => "În progres",
            RequestStatus.Approved => "Aprobat",
            RequestStatus.Rejected => "Respins",
            RequestStatus.Completed => "Finalizat",
            _ => status.ToString()
        };
    }

    private static string? ExtractRejectionReason(string notes)
    {
        // Try to extract rejection reason from notes
        if (notes.Contains("Respins: "))
        {
            var startIndex = notes.IndexOf("Respins: ") + "Respins: ".Length;
            var endIndex = notes.IndexOf('\n', startIndex);
            return endIndex > startIndex
                ? notes.Substring(startIndex, endIndex - startIndex)
                : notes.Substring(startIndex);
        }
        return null;
    }
}
