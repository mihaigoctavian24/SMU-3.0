using SMU.Data.Entities;

namespace SMU.Services;

/// <summary>
/// DTOs for risk scoring service
/// </summary>
public class RiskFactorsDto
{
    public Guid StudentId { get; set; }
    public decimal CurrentGradeScore { get; set; }
    public decimal GradeTrendScore { get; set; }
    public decimal AttendanceScore { get; set; }
    public decimal ConsecutiveAbsencesScore { get; set; }
    public decimal FailedCoursesScore { get; set; }
    public decimal EngagementScore { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}

public class RiskDistributionDto
{
    public int TotalStudents { get; set; }
    public int LowRisk { get; set; }
    public int MediumRisk { get; set; }
    public int HighRisk { get; set; }
    public int CriticalRisk { get; set; }
    public decimal LowRiskPercentage { get; set; }
    public decimal MediumRiskPercentage { get; set; }
    public decimal HighRiskPercentage { get; set; }
    public decimal CriticalRiskPercentage { get; set; }
}

/// <summary>
/// Service for calculating student risk scores and predictive analytics
/// </summary>
public interface IRiskScoringService
{
    /// <summary>
    /// Calculate comprehensive risk score for a student
    /// </summary>
    Task<StudentRiskScore> CalculateStudentRiskAsync(Guid studentId);

    /// <summary>
    /// Get detailed risk factors breakdown for a student
    /// </summary>
    Task<RiskFactorsDto> GetRiskFactorsAsync(Guid studentId);

    /// <summary>
    /// Get recommended actions based on risk level
    /// </summary>
    Task<List<string>> GetRecommendationsAsync(RiskLevel riskLevel);

    /// <summary>
    /// Bulk calculate risk scores for all students or faculty students
    /// </summary>
    /// <param name="facultyId">Optional faculty filter</param>
    /// <returns>Number of scores calculated</returns>
    Task<int> BulkCalculateRisksAsync(Guid? facultyId = null);

    /// <summary>
    /// Get list of high-risk students requiring intervention
    /// </summary>
    Task<List<StudentRiskScore>> GetHighRiskStudentsAsync(Guid? facultyId = null, int limit = 20);

    /// <summary>
    /// Get risk distribution statistics
    /// </summary>
    Task<RiskDistributionDto> GetRiskDistributionAsync(Guid? facultyId = null);
}
