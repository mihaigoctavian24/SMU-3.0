namespace SMU.Services.Jobs;

/// <summary>
/// Interface for scheduled background jobs
/// </summary>
public interface IScheduledJob
{
    /// <summary>
    /// Execute the job
    /// </summary>
    Task ExecuteAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Unique name for this job
    /// </summary>
    string JobName { get; }

    /// <summary>
    /// How often this job should run
    /// </summary>
    TimeSpan Interval { get; }
}
