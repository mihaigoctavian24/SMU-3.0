using SMU.Data.Entities;

namespace SMU.Services;

/// <summary>
/// Service interface for risk alert management and stakeholder notifications
/// Supports Faza 8: Predictive Analytics - Early Warning System
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// Create a new risk alert for a student
    /// </summary>
    /// <param name="studentId">Student identifier</param>
    /// <param name="riskLevel">Severity level of the risk</param>
    /// <param name="alertType">Type of alert (RiskScoreHigh, ConsecutiveAbsences, etc.)</param>
    /// <param name="message">Alert message content</param>
    /// <returns>Created RiskAlert entity</returns>
    Task<RiskAlert> CreateRiskAlertAsync(Guid studentId, RiskLevel riskLevel, string alertType, string message);

    /// <summary>
    /// Notify appropriate stakeholders based on alert severity
    /// - Risk Score > 60: Notify tutor + secretariat
    /// - Risk Score > 80: Notify dean + email student
    /// - Consecutive absences > 5: Alert professor + secretariat
    /// </summary>
    /// <param name="alertId">Alert identifier</param>
    Task NotifyStakeholdersAsync(Guid alertId);

    /// <summary>
    /// Track intervention progress and update alert status
    /// </summary>
    /// <param name="alertId">Alert identifier</param>
    /// <param name="notes">Intervention notes</param>
    /// <param name="status">Current intervention status</param>
    /// <returns>Updated RiskAlert entity</returns>
    Task<RiskAlert> TrackInterventionAsync(Guid alertId, string notes, string status);

    /// <summary>
    /// Mark an alert as acknowledged by a user
    /// </summary>
    /// <param name="alertId">Alert identifier</param>
    /// <param name="acknowledgedByUserId">User who acknowledged the alert</param>
    Task AcknowledgeAlertAsync(Guid alertId, Guid acknowledgedByUserId);

    /// <summary>
    /// Get all active (unacknowledged) alerts, optionally filtered by faculty
    /// </summary>
    /// <param name="facultyId">Optional faculty filter</param>
    /// <returns>List of active risk alerts</returns>
    Task<List<RiskAlert>> GetActiveAlertsAsync(Guid? facultyId = null);

    /// <summary>
    /// Get all alerts for a specific student
    /// </summary>
    /// <param name="studentId">Student identifier</param>
    /// <returns>List of student's risk alerts</returns>
    Task<List<RiskAlert>> GetStudentAlertsAsync(Guid studentId);

    /// <summary>
    /// Check student metrics and create auto-alerts based on thresholds
    /// Triggers:
    /// - Risk Score > 60 → Medium alert
    /// - Risk Score > 80 → Critical alert
    /// - Consecutive absences > 5 → High alert
    /// </summary>
    /// <param name="studentId">Student identifier</param>
    Task CheckAndCreateAutoAlertsAsync(Guid studentId);
}
