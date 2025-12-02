using WindLordApi.Worker;
using WindLordApi.Worker.Services;
using WindLordApi.Worker.Startup;
using WindLordApi.Data;
using WindLordApi.Data.Extensions;
using WindLordApi.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using WindLordApi.Integrations.MetFrost;

static async Task CheckPendingMigrationsAsync(IHost host)
{
    using var scope = host.Services.CreateScope();
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("MigrationHealthCheck");
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

        if (pendingMigrations.Any())
        {
            logger.LogError(
                "⚠️  PENDING MIGRATIONS DETECTED! The following migrations have not been applied: {PendingMigrations}",
                string.Join(", ", pendingMigrations));

            logger.LogError(
                "Please run the following command to apply migrations: dotnet ef database update --project src/WindLordApi.Data/WindLordApi.Data.csproj");

            // Optionally fail startup - uncomment the line below to prevent startup with pending migrations
            // throw new InvalidOperationException($"Cannot start application with {pendingMigrations.Count()} pending migration(s). Please apply migrations first.");
        }
        else
        {
            logger.LogInformation("✅ Database migrations are up to date.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Failed to check for pending migrations. The application will continue, but please verify database connectivity.");
        // Don't throw - allow application to start even if migration check fails
        // This prevents connection issues from blocking startup
    }
}


var builder = Host.CreateApplicationBuilder(args);

// Explicitly add user secrets (normally only loaded in Development)
// This allows Production debugging to access user secrets
builder.Configuration.AddUserSecrets(typeof(Program).Assembly);

// Explicitly configure logging to prevent duplicates
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = false;
    options.SingleLine = false;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

// Register DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Get connection string using extension method (handles environment detection automatically)
    var connectionString = builder.Configuration.GetSupabaseConnectionString(builder.Environment);
    options.UseNpgsql(connectionString);
});

// Register Data Services
builder.Services.AddScoped<IStationDataService, StationDataService>();
builder.Services.AddScoped<IWeatherStationService, WeatherStationService>();

// Register Worker Services
builder.Services.AddScoped<IMetFrostSyncService, MetFrostSyncService>();

// Register MET Frost Client
// 1. Configure Options (binds from appsettings)
builder.Services.Configure<MetFrostOptions>(
    builder.Configuration.GetSection(MetFrostOptions.SectionName));

// 2. Register HttpClient + Service together (recommended)
builder.Services.AddHttpClient<IMetFrostClient, MetFrostClient>();

// Register Health Check Services
builder.Services.AddScoped<DatabaseHealthCheck>();
builder.Services.AddScoped<MetFrostHealthCheck>();

// Register Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "db", "database" })
    .AddCheck<MetFrostHealthCheck>("metfrost", tags: new[] { "api", "metfrost" });

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

// Check for pending migrations on startup
await CheckPendingMigrationsAsync(host);

// Run health checks on startup
var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger("Startup");
await HealthCheck.RunHealthChecksAsync(host.Services, logger);

host.Run();
