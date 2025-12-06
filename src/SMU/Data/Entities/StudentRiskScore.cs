namespace SMU.Data.Entities;

/// <summary>
/// Student risk score for early warning system
/// Calculated automatically based on multiple risk factors
/// </summary>
public class StudentRiskScore
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public int RiskScore { get; set; } // 0-100, mapped to risk_score column
    public RiskLevel RiskLevel { get; set; } // mapped to risk_level column
    public decimal? DropoutProbability { get; set; }
    public decimal? FailureProbability { get; set; }
    public string? Factors { get; set; } // JSON - factors column
    public string? Recommendations { get; set; } // JSON array of suggested actions
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ValidUntil { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Risk factor breakdown
    public decimal? AttendanceRiskFactor { get; set; }
    public decimal? GradeRiskFactor { get; set; }
    public decimal? TrendRiskFactor { get; set; }
    public decimal? EngagementRiskFactor { get; set; }

    // Navigation
    public Student Student { get; set; } = null!;
}
