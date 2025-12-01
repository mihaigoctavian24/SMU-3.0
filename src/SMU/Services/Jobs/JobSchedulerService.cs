namespace SMU.Services.Jobs;

/// <summary>
/// Hosted service that manages and executes scheduled background jobs
/// </summary>
public class JobSchedulerService : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobSchedulerService> _logger;
    private readonly IHostEnvironment _environment;
    private readonly List<IScheduledJob> _jobs;
    private readonly Dictionary<string, DateTime> _lastExecutionTimes;
    private Timer? _timer;

    public JobSchedulerService(
        IServiceProvider serviceProvider,
        ILogger<JobSchedulerService> logger,
        IHostEnvironment environment)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _environment = environment;
        _jobs = new List<IScheduledJob>();
        _lastExecutionTimes = new Dictionary<string, DateTime>();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("JobSchedulerService is starting");

        // Register jobs - create directly with root service provider
        // Jobs will create their own scopes internally for database access
        var dailySnapshotLogger = _serviceProvider.GetRequiredService<ILogger<DailySnapshotJob>>();
        var riskCalculationLogger = _serviceProvider.GetRequiredService<ILogger<RiskCalculationJob>>();

        _jobs.Add(new DailySnapshotJob(_serviceProvider, dailySnapshotLogger));
        _jobs.Add(new RiskCalculationJob(_serviceProvider, riskCalculationLogger));

        _logger.LogInformation("Registered {Count} scheduled jobs", _jobs.Count);

        // In development, run jobs immediately for testing
        if (_environment.IsDevelopment())
        {
            _logger.LogInformation("Development mode: Running jobs immediately");
            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken); // Small delay for app startup
                await ExecuteAllJobsAsync(cancellationToken);
            }, cancellationToken);
        }

        // Start timer to check every hour
        _timer = new Timer(
            DoWork,
            null,
            TimeSpan.Zero,
            TimeSpan.FromHours(1));

        return Task.CompletedTask;
    }

    private void DoWork(object? state)
    {
        _logger.LogDebug("JobScheduler tick - checking for jobs to execute");
        Task.Run(async () => await CheckAndExecuteJobsAsync(CancellationToken.None));
    }

    private async Task CheckAndExecuteJobsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        foreach (var job in _jobs)
        {
            try
            {
                // Check if job should run
                if (!_lastExecutionTimes.TryGetValue(job.JobName, out var lastExecution))
                {
                    // Never run before, run now
                    await ExecuteJobAsync(job, cancellationToken);
                    continue;
                }

                var timeSinceLastRun = now - lastExecution;
                if (timeSinceLastRun >= job.Interval)
                {
                    // Time to run again
                    await ExecuteJobAsync(job, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking job {JobName}", job.JobName);
            }
        }
    }

    private async Task ExecuteAllJobsAsync(CancellationToken cancellationToken)
    {
        foreach (var job in _jobs)
        {
            await ExecuteJobAsync(job, cancellationToken);
        }
    }

    private async Task ExecuteJobAsync(IScheduledJob job, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Executing job: {JobName}", job.JobName);

        try
        {
            await job.ExecuteAsync(cancellationToken);

            _lastExecutionTimes[job.JobName] = DateTime.UtcNow;

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "Job {JobName} completed successfully in {Duration}ms",
                job.JobName,
                duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex,
                "Job {JobName} failed after {Duration}ms",
                job.JobName,
                duration.TotalMilliseconds);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("JobSchedulerService is stopping");

        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
