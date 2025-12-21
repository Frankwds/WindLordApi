using WindLordApi.Worker;
using WindLordApi.Worker.Schedulers;
using WindLordApi.Worker.Services;
using WindLordApi.Worker.Startup;
using WindLordApi.Data;
using WindLordApi.Data.Extensions;
using WindLordApi.Data.Services;
using WindLordApi.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using WindLordApi.Integrations.MetFrost;
using WindLordApi.Integrations.Holfuy;
using WindLordApi.Integrations.MetYr;
using WindLordApi.Integrations.OpenMeteo;
using Serilog;
using Serilog.Events;

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


// Configure Serilog before building the host
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProperty("Application", "WindLordApi.Worker")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/windlordapi-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);

// Explicitly add user secrets (normally only loaded in Development)
// This allows Production debugging to access user secrets
builder.Configuration.AddUserSecrets(typeof(Program).Assembly);

// Use Serilog for logging
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

// Register DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Get connection string using extension method (handles environment detection automatically)
    var connectionString = builder.Configuration.GetSupabaseConnectionString(builder.Environment);
    options.UseNpgsql(connectionString);
});

// Register Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register Data Services
builder.Services.AddScoped<IStationDataService, StationDataService>();
builder.Services.AddScoped<IWeatherStationService, WeatherStationService>();
builder.Services.AddScoped<ILatestStationDataService, LatestStationDataService>();
builder.Services.AddScoped<IParaglidingLocationService, ParaglidingLocationService>();
builder.Services.AddScoped<IForecastCacheService, ForecastCacheService>();

// Register Mapping Services
builder.Services.AddScoped<IMetFrostMapping, MetFrostMappingService>();
builder.Services.AddScoped<IHolfuyMapping, HolfuyMappingService>();
builder.Services.AddScoped<IMetYrMapping, MetYrMappingService>();
builder.Services.AddScoped<IOpenMeteoMapping, OpenMeteoMappingService>();

// Register Worker Services
builder.Services.AddScoped<IHolfuySyncService, HolfuySyncService>();
builder.Services.AddScoped<IMetFrostSyncService, MetFrostSyncService>();
builder.Services.AddScoped<IForecastCombinationService, ForecastCombinationService>();
builder.Services.AddScoped<IForecastUpdateService, ForecastUpdateService>();

// Register Schedulers (singleton since Worker is singleton)
builder.Services.AddSingleton<CronScheduler<IMetFrostSyncService>>();
builder.Services.AddSingleton<CronScheduler<IHolfuySyncService>>();
builder.Services.AddSingleton<CronScheduler<IForecastUpdateService>>();

// Register MET Frost Client
// 1. Configure Options (binds from appsettings) with validation
builder.Services.AddOptions<MetFrostOptions>()
    .Bind(builder.Configuration.GetSection(MetFrostOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// 2. Register HttpClient + Service together (recommended)
builder.Services.AddHttpClient<IMetFrostClient, MetFrostClient>();

// Register Holfuy Client
builder.Services.AddHolfuyClient(builder.Configuration);

// Register MetYr Client
builder.Services.AddMetYrClient(builder.Configuration);

// Register OpenMeteo Client
builder.Services.AddOpenMeteoClient(builder.Configuration);

// Register Health Check Services
builder.Services.AddScoped<DatabaseHealthCheck>();
builder.Services.AddScoped<MetFrostHealthCheck>();
builder.Services.AddScoped<HolfuyHealthCheck>();
builder.Services.AddScoped<MetYrHealthCheck>();
builder.Services.AddScoped<OpenMeteoHealthCheck>();

// Register Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["db", "database"])
    .AddCheck<MetFrostHealthCheck>("metfrost", tags: ["api", "metfrost"])
    .AddCheck<HolfuyHealthCheck>("holfuy", tags: ["api", "holfuy"])
    .AddCheck<MetYrHealthCheck>("metyr", tags: ["api", "metyr"])
    .AddCheck<OpenMeteoHealthCheck>("openmeteo", tags: ["api", "openmeteo"]);

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

try
{
    Log.Information("Starting WindLordApi.Worker application");

    // Check for pending migrations on startup
    await CheckPendingMigrationsAsync(host);

    // Run health checks on startup
    var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("Startup");
    await HealthCheck.RunHealthChecksAsync(host.Services, logger);

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
