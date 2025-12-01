using Microsoft.EntityFrameworkCore;
using SMU.Data;
using SMU.Data.Entities;

namespace SMU.Services;

/// <summary>
/// Alert service implementation for risk notifications and stakeholder alerting
/// Supports automated early warning system for at-risk students
/// </summary>
public class AlertService : IAlertService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<AlertService> _logger;

    public AlertService(
        ApplicationDbContext context,
        INotificationService notificationService,
        ILogger<AlertService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<RiskAlert> CreateRiskAlertAsync(Guid studentId, RiskLevel riskLevel, string alertType, string message)
    {
        try
        {
            var alert = new RiskAlert
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                RiskLevel = riskLevel,
                AlertType = alertType,
                Message = message,
                IsAcknowledged = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.RiskAlerts.Add(alert);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Risk alert created: {AlertId} for student {StudentId}, Level: {RiskLevel}, Type: {AlertType}",
                alert.Id, studentId, riskLevel, alertType);

            return alert;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating risk alert for student {StudentId}", studentId);
            throw;
        }
    }

    public async Task NotifyStakeholdersAsync(Guid alertId)
    {
        try
        {
            var alert = await _context.RiskAlerts
                .Include(a => a.Student)
                    .ThenInclude(s => s.User)
                .Include(a => a.Student)
                    .ThenInclude(s => s.Group)
                        .ThenInclude(g => g!.Program)
                            .ThenInclude(p => p.Faculty)
                .FirstOrDefaultAsync(a => a.Id == alertId);

            if (alert == null)
            {
                _logger.LogWarning("Alert {AlertId} not found for stakeholder notification", alertId);
                return;
            }

            var student = alert.Student;
            var faculty = student.Group?.Program?.Faculty;

            // Notification title and message
            var title = $"Alertă Risc: {student.User.FirstName} {student.User.LastName}";
            var message = alert.Message;
            var notificationType = alert.RiskLevel switch
            {
                RiskLevel.Critical => NotificationType.Error,
                RiskLevel.High => NotificationType.Warning,
                _ => NotificationType.Warning
            };

            // Determine stakeholders based on risk level and alert type
            var stakeholderUserIds = new List<Guid>();

            // Always notify secretariat for faculty
            if (faculty != null)
            {
                var secretaries = await _context.Users
                    .Include(u => u.Professor)
                    .Where(u => u.Role == UserRole.Secretary &&
                           u.Professor != null && u.Professor.FacultyId == faculty.Id)
                    .Select(u => u.Id)
                    .ToListAsync();

                stakeholderUserIds.AddRange(secretaries);
            }

            // High and Critical: Notify dean
            if (alert.RiskLevel >= RiskLevel.High && faculty?.DeanId != null)
            {
                stakeholderUserIds.Add(faculty.DeanId.Value);
            }

            // Critical: Notify student directly
            if (alert.RiskLevel == RiskLevel.Critical)
            {
                stakeholderUserIds.Add(student.UserId);
            }

            // Consecutive absences: Notify professors of the courses
            if (alert.AlertType == "ConsecutiveAbsences")
            {
                var professorIds = await _context.Courses
                    .Where(c => c.ProgramId == student.Group!.ProgramId && c.ProfessorId != null)
                    .Select(c => c.Professor!.UserId)
                    .Distinct()
                    .ToListAsync();

                stakeholderUserIds.AddRange(professorIds);
            }

            // Remove duplicates
            stakeholderUserIds = stakeholderUserIds.Distinct().ToList();

            // Send notifications to all stakeholders
            foreach (var userId in stakeholderUserIds)
            {
                await _notificationService.SendAsync(
                    userId,
                    title,
                    message,
                    notificationType,
                    link: $"/students/{student.Id}/alerts"
                );
            }

            _logger.LogInformation(
                "Stakeholder notifications sent for alert {AlertId}: {Count} recipients",
                alertId, stakeholderUserIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying stakeholders for alert {AlertId}", alertId);
            throw;
        }
    }

    public async Task<RiskAlert> TrackInterventionAsync(Guid alertId, string notes, string status)
    {
        try
        {
            var alert = await _context.RiskAlerts.FindAsync(alertId);

            if (alert == null)
            {
                throw new InvalidOperationException($"Alert {alertId} not found");
            }

            alert.InterventionNotes = notes;
            alert.InterventionStatus = status;
            alert.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Intervention tracked for alert {AlertId}: Status = {Status}",
                alertId, status);

            return alert;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking intervention for alert {AlertId}", alertId);
            throw;
        }
    }

    public async Task AcknowledgeAlertAsync(Guid alertId, Guid acknowledgedByUserId)
    {
        try
        {
            var alert = await _context.RiskAlerts.FindAsync(alertId);

            if (alert == null)
            {
                _logger.LogWarning("Alert {AlertId} not found for acknowledgment", alertId);
                return;
            }

            alert.IsAcknowledged = true;
            alert.AcknowledgedBy = acknowledgedByUserId;
            alert.AcknowledgedAt = DateTime.UtcNow;
            alert.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Alert {AlertId} acknowledged by user {UserId}",
                alertId, acknowledgedByUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging alert {AlertId}", alertId);
            throw;
        }
    }

    public async Task<List<RiskAlert>> GetActiveAlertsAsync(Guid? facultyId = null)
    {
        try
        {
            var query = _context.RiskAlerts
                .Include(a => a.Student)
                    .ThenInclude(s => s.User)
                .Include(a => a.Student)
                    .ThenInclude(s => s.Group)
                        .ThenInclude(g => g!.Program)
                .Where(a => !a.IsAcknowledged)
                .AsQueryable();

            if (facultyId.HasValue)
            {
                query = query.Where(a => a.Student.Group!.Program.FacultyId == facultyId.Value);
            }

            var alerts = await query
                .OrderByDescending(a => a.RiskLevel)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            return alerts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active alerts for faculty {FacultyId}", facultyId);
            throw;
        }
    }

    public async Task<List<RiskAlert>> GetStudentAlertsAsync(Guid studentId)
    {
        try
        {
            var alerts = await _context.RiskAlerts
                .Include(a => a.AcknowledgedByUser)
                .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return alerts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alerts for student {StudentId}", studentId);
            throw;
        }
    }

    public async Task CheckAndCreateAutoAlertsAsync(Guid studentId)
    {
        try
        {
            // Get latest risk score
            var riskScore = await _context.StudentRiskScores
                .Where(r => r.StudentId == studentId)
                .OrderByDescending(r => r.CalculatedAt)
                .FirstOrDefaultAsync();

            // Get attendance stats
            var attendanceStats = await _context.AttendanceStats
                .Where(a => a.StudentId == studentId)
                .ToListAsync();

            var maxConsecutiveAbsences = attendanceStats.Any()
                ? attendanceStats.Max(a => a.ConsecutiveAbsences)
                : 0;

            // Check for existing recent alerts to avoid duplicates (within last 7 days)
            var recentAlerts = await _context.RiskAlerts
                .Where(a => a.StudentId == studentId && a.CreatedAt >= DateTime.UtcNow.AddDays(-7))
                .ToListAsync();

            // Risk Score > 80 → Critical alert
            if (riskScore != null && riskScore.OverallScore > 80)
            {
                if (!recentAlerts.Any(a => a.AlertType == "RiskScoreCritical"))
                {
                    var alert = await CreateRiskAlertAsync(
                        studentId,
                        RiskLevel.Critical,
                        "RiskScoreCritical",
                        $"Scor de risc critic: {riskScore.OverallScore}/100. Necesită intervenție urgentă."
                    );

                    await NotifyStakeholdersAsync(alert.Id);
                }
            }
            // Risk Score > 60 → High alert
            else if (riskScore != null && riskScore.OverallScore > 60)
            {
                if (!recentAlerts.Any(a => a.AlertType == "RiskScoreHigh"))
                {
                    var alert = await CreateRiskAlertAsync(
                        studentId,
                        RiskLevel.High,
                        "RiskScoreHigh",
                        $"Scor de risc ridicat: {riskScore.OverallScore}/100. Se recomandă monitorizare atentă."
                    );

                    await NotifyStakeholdersAsync(alert.Id);
                }
            }

            // Consecutive absences > 5 → High alert
            if (maxConsecutiveAbsences > 5)
            {
                if (!recentAlerts.Any(a => a.AlertType == "ConsecutiveAbsences"))
                {
                    var alert = await CreateRiskAlertAsync(
                        studentId,
                        RiskLevel.High,
                        "ConsecutiveAbsences",
                        $"Absențe consecutive: {maxConsecutiveAbsences}. Risc de abandon academic."
                    );

                    await NotifyStakeholdersAsync(alert.Id);
                }
            }

            _logger.LogInformation(
                "Auto-alert check completed for student {StudentId}: Risk Score = {Score}, Max Consecutive Absences = {Absences}",
                studentId, riskScore?.OverallScore ?? 0, maxConsecutiveAbsences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking auto-alerts for student {StudentId}", studentId);
            throw;
        }
    }
}
