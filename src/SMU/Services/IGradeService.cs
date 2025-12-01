using SMU.Services.DTOs;

namespace SMU.Services;

/// <summary>
/// Interface for grade management operations
/// </summary>
public interface IGradeService
{
    /// <summary>
    /// Get all grades for a specific student with course details
    /// </summary>
    Task<List<GradeListDto>> GetByStudentAsync(Guid studentId);

    /// <summary>
    /// Get all grades for a specific course with student details
    /// </summary>
    Task<List<GradeListDto>> GetByCourseAsync(Guid courseId);

    /// <summary>
    /// Get pending grades awaiting approval, optionally filtered by faculty
    /// </summary>
    Task<List<GradeListDto>> GetPendingForApprovalAsync(Guid? facultyId = null);

    /// <summary>
    /// Get detailed information about a specific grade
    /// </summary>
    Task<GradeDetailDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Create a new grade with Pending status
    /// </summary>
    Task<GradeResult> CreateAsync(CreateGradeDto dto, Guid enteredById);

    /// <summary>
    /// Update an existing grade (only if status is Pending)
    /// </summary>
    Task<GradeResult> UpdateAsync(Guid id, UpdateGradeDto dto);

    /// <summary>
    /// Approve a grade (Dean role)
    /// </summary>
    Task<GradeResult> ApproveAsync(Guid id, Guid approvedById);

    /// <summary>
    /// Reject a grade with reason (Dean role)
    /// </summary>
    Task<GradeResult> RejectAsync(Guid id, Guid rejectedById, string reason);

    /// <summary>
    /// Bulk create grades for multiple students in a course
    /// </summary>
    Task<GradeResult> BulkCreateAsync(List<CreateGradeDto> grades, Guid enteredById);

    /// <summary>
    /// Calculate weighted average grade for a student based on credits
    /// </summary>
    Task<StudentAverageDto?> GetStudentAverageAsync(Guid studentId);

    /// <summary>
    /// Delete a grade (only if status is Pending)
    /// </summary>
    Task<GradeResult> DeleteAsync(Guid id);
}

/// <summary>
/// Result object for grade operations
/// </summary>
public class GradeResult
{
    public bool Succeeded { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Guid? GradeId { get; private set; }

    public static GradeResult Success(Guid? gradeId = null) => new()
    {
        Succeeded = true,
        GradeId = gradeId
    };

    public static GradeResult Failed(string error) => new()
    {
        Succeeded = false,
        ErrorMessage = error
    };
}
