namespace SMU.Data.Entities;

/// <summary>
/// Student risk score for early warning system
/// Calculated automatically based on multiple risk factors
/// </summary>
public class StudentRiskScore
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public int OverallScore { get; set; } // 0-100
    public RiskLevel Level { get; set; }
    public decimal? GradeRiskFactor { get; set; }
    public decimal? AttendanceRiskFactor { get; set; }
    public decimal? TrendRiskFactor { get; set; }
    public decimal? EngagementRiskFactor { get; set; }
    public string? RiskFactors { get; set; } // JSON array of specific risk reasons
    public string? Recommendations { get; set; } // JSON array of suggested actions
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedById { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Student Student { get; set; } = null!;
    public ApplicationUser? ReviewedBy { get; set; }
}
