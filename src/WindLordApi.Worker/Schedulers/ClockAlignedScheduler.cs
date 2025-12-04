using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WindLordApi.Worker.Services;

namespace WindLordApi.Worker.Schedulers;

/// <summary>
/// Scheduler that runs jobs at clock-aligned intervals (e.g., :00, :15, :30, :45) 
/// with a specified offset delay after each interval mark.
/// Used for Holfuy sync which releases data at these intervals.
/// </summary>
public class ClockAlignedScheduler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ClockAlignedScheduler> _logger;

    public ClockAlignedScheduler(
        IServiceScopeFactory scopeFactory,
        ILogger<ClockAlignedScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Runs a job at clock-aligned intervals with a specified offset.
    /// For example, with a 15-minute interval and 15-second offset:
    /// - If current time is 10:07:30, next run is 10:15:15
    /// - If current time is 10:15:00, next run is 10:15:15
    /// - If current time is 10:15:20, next run is 10:30:15
    /// </summary>
    /// <param name="interval">The interval between runs (e.g., 15 minutes)</param>
    /// <param name="offset">The delay after each interval mark (e.g., 15 seconds)</param>
    /// <param name="jobName">Name of the job for logging</param>
    /// <param name="jobAction">The job to execute</param>
    /// <param name="stoppingToken">Cancellation token</param>
    public async Task RunAsync(
        TimeSpan interval,
        TimeSpan offset,
        string jobName,
        Func<IHolfuySyncService, CancellationToken, Task<int>> jobAction,
        CancellationToken stoppingToken)
    {
        try
        {
            // Calculate and wait until the next scheduled run time
            var nextRunTime = ClockAlignedSchedulerHelper.CalculateNextScheduledRunTime(interval, offset);
            var delayUntilNextRun = nextRunTime - DateTimeOffset.UtcNow;

            if (delayUntilNextRun > TimeSpan.Zero)
            {
                _logger.LogInformation(
                    "Job {JobName} will start at {ScheduledTime} (in {DelaySeconds:F1} seconds)",
                    jobName,
                    nextRunTime.ToString("HH:mm:ss"),
                    delayUntilNextRun.TotalSeconds);

                await Task.Delay(delayUntilNextRun, stoppingToken);
            }

            // Create periodic timer that will tick every interval (15 minutes)
            using var timer = new PeriodicTimer(interval);

            // Run the job immediately (we're now at a scheduled time)
            await ExecuteJobOnceAsync(jobName, jobAction, stoppingToken);

            // Continue with periodic execution every interval
            while (!stoppingToken.IsCancellationRequested)
            {
                var hasNextTick = await timer.WaitForNextTickAsync(stoppingToken);
                if (!hasNextTick)
                {
                    break; // Timer was disposed
                }

                await ExecuteJobOnceAsync(jobName, jobAction, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Scheduled job {JobName} was cancelled", jobName);
        }
    }

    private async Task ExecuteJobOnceAsync(
        string jobName,
        Func<IHolfuySyncService, CancellationToken, Task<int>> jobAction,
        CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting scheduled job: {JobName}", jobName);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IHolfuySyncService>();
            var count = await jobAction(syncService, stoppingToken);
            _logger.LogInformation("Completed scheduled job: {JobName} (inserted {Count} new records)", jobName, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in scheduled job: {JobName}", jobName);
            // Continue to next iteration - don't stop the timer
        }
    }
}

