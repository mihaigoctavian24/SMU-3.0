using Microsoft.EntityFrameworkCore;
using SMU.Data;
using SMU.Data.Entities;

namespace SMU.Services.Jobs;

/// <summary>
/// Weekly job to recalculate student risk scores and create auto-alerts
/// </summary>
public class RiskCalculationJob : IScheduledJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RiskCalculationJob> _logger;

    public string JobName => "RiskCalculation";
    public TimeSpan Interval => TimeSpan.FromDays(7); // Weekly

    public RiskCalculationJob(
        IServiceProvider serviceProvider,
        ILogger<RiskCalculationJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting RiskCalculationJob execution");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var riskService = scope.ServiceProvider.GetRequiredService<IRiskScoringService>();
            var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Recalculate risks for all students
            _logger.LogInformation("Recalculating risk scores for all students");
            await riskService.BulkCalculateRisksAsync(null);

            // Get high-risk students (risk score >= 60)
            var highRiskStudents = await dbContext.StudentRiskScores
                .Where(ra => ra.RiskScore >= 60)
                .OrderByDescending(ra => ra.RiskScore)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} high-risk students", highRiskStudents.Count);

            // Create auto-alerts for high-risk students
            foreach (var riskAssessment in highRiskStudents)
            {
                try
                {
                    await alertService.CheckAndCreateAutoAlertsAsync(
                        riskAssessment.StudentId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error creating auto-alert for student {StudentId}",
                        riskAssessment.StudentId);
                    // Continue with other students
                }
            }

            _logger.LogInformation("RiskCalculationJob completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing RiskCalculationJob");
            throw;
        }
    }
}
