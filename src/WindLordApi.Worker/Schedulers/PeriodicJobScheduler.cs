using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WindLordApi.Worker.Services;

namespace WindLordApi.Worker.Schedulers;

/// <summary>
/// Scheduler that runs jobs at regular periodic intervals using a PeriodicTimer.
/// </summary>
public class PeriodicJobScheduler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PeriodicJobScheduler> _logger;

    public PeriodicJobScheduler(
        IServiceProvider serviceProvider,
        ILogger<PeriodicJobScheduler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Runs a job periodically at the specified interval.
    /// </summary>
    /// <param name="timer">The periodic timer to use</param>
    /// <param name="jobAction">The job to execute</param>
    /// <param name="jobName">Name of the job for logging</param>
    /// <param name="stoppingToken">Cancellation token</param>
    public async Task RunAsync(
        PeriodicTimer timer,
        Func<IMetFrostSyncService, CancellationToken, Task> jobAction,
        string jobName,
        CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait for the next timer tick
                var hasNextTick = await timer.WaitForNextTickAsync(stoppingToken);
                if (!hasNextTick)
                {
                    break; // Timer was disposed
                }

                _logger.LogInformation("Starting scheduled job: {JobName}", jobName);

                try
                {
                    // Create a new scope for each job execution
                    using var scope = _serviceProvider.CreateScope();
                    var syncService = scope.ServiceProvider.GetRequiredService<IMetFrostSyncService>();
                    await jobAction(syncService, stoppingToken);
                    _logger.LogInformation("Completed scheduled job: {JobName}", jobName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in scheduled job: {JobName}", jobName);
                    // Continue to next iteration - don't stop the timer
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Scheduled job {JobName} was cancelled", jobName);
        }
        finally
        {
            timer.Dispose();
        }
    }
}

