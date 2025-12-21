using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WindLordApi.Worker.Schedulers;

/// <summary>
/// Scheduler that runs jobs based on cron expressions.
/// </summary>
/// <typeparam name="TService">The service type to resolve from dependency injection.</typeparam>
public class CronScheduler<TService> where TService : class
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CronScheduler<TService>> _logger;

    public CronScheduler(
        IServiceScopeFactory scopeFactory,
        ILogger<CronScheduler<TService>> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Runs a job based on a cron expression schedule.
    /// </summary>
    /// <param name="cronExpression">The cron expression (with seconds support)</param>
    /// <param name="jobAction">The job to execute</param>
    /// <param name="jobName">Name of the job for logging</param>
    /// <param name="stoppingToken">Cancellation token</param>
    public async Task RunAsync(
        string cronExpression,
        Func<TService, CancellationToken, Task> jobAction,
        string jobName,
        CancellationToken stoppingToken)
    {
        CronExpression expression;
        try
        {
            // Parse with seconds support (6-part cron expression)
            expression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invalid cron expression '{CronExpression}' for job {JobName}", cronExpression, jobName);
            throw;
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Calculate next occurrence
                var now = DateTimeOffset.UtcNow;
                var nextOccurrence = expression.GetNextOccurrence(now, TimeZoneInfo.Utc);

                if (nextOccurrence == null)
                {
                    _logger.LogWarning("No next occurrence found for job {JobName} with cron expression {CronExpression}",
                        jobName, cronExpression);
                    break;
                }

                var delay = nextOccurrence.Value - now;

                if (delay > TimeSpan.Zero)
                {
                    _logger.LogDebug(
                        "Job {JobName} will run at {ScheduledTime} (in {DelaySeconds:F1} seconds)",
                        jobName,
                        nextOccurrence.Value.ToString("HH:mm:ss"),
                        delay.TotalSeconds);

                    await Task.Delay(delay, stoppingToken);
                }

                // Execute the job
                await ExecuteJobOnceAsync(jobName, jobAction, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Scheduled job {JobName} was cancelled", jobName);
        }
    }

    /// <summary>
    /// Calculates the next scheduled run time for a cron expression.
    /// </summary>
    /// <param name="cronExpression">The cron expression (with seconds support)</param>
    /// <returns>The next scheduled run time, or null if no occurrence found</returns>
    public static DateTimeOffset? CalculateNextRunTime(string cronExpression)
    {
        try
        {
            var expression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);
            return expression.GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Utc);
        }
        catch
        {
            return null;
        }
    }

    private async Task ExecuteJobOnceAsync(
        string jobName,
        Func<TService, CancellationToken, Task> jobAction,
        CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting scheduled job: {JobName}", jobName);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<TService>();
            await jobAction(service, stoppingToken);
            _logger.LogInformation("Completed scheduled job: {JobName}", jobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in scheduled job: {JobName}", jobName);
            // Continue to next iteration - don't stop the scheduler
        }
    }
}

