using WindLordApi.Data;
using Microsoft.EntityFrameworkCore;

namespace WindLordApi.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public Worker(
        ILogger<Worker> logger,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Load the connection string from configuration
        var connectionString = _configuration.GetConnectionString("SUPABASE_CONNECTION_STRING")
            ?? throw new InvalidOperationException("Supabase connection string not found");

        _logger.LogInformation("DB Connection String configured: {HasConnection}",
            connectionString != "Supabase connection string not found");

        // Test database connection
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Test the connection
            var canConnect = await dbContext.Database.CanConnectAsync(stoppingToken);

            if (canConnect)
            {
                _logger.LogInformation("Successfully connected to database!");
            }
            else
            {
                _logger.LogWarning("Cannot connect to database");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to database");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            await Task.Delay(10000, stoppingToken);
        }
    }
}
