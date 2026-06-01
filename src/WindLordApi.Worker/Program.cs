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
using WindLordApi.Integrations.WindsMobi;
using WindLordApi.Integrations.PortWind;
using WindLordApi.Integrations.GoogleGeocoding;
using Serilog;
using Serilog.Events;


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

// Allow a dedicated Local environment file to override user secrets when debugging
// against the local Supabase stack.
if (builder.Environment.IsEnvironment("Local"))
{
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
}

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

// Register Worker Services
builder.Services.AddScoped<IHolfuySyncService, HolfuySyncService>();
builder.Services.AddScoped<IMetFrostSyncService, MetFrostSyncService>();
builder.Services.AddScoped<IPortWindStationRefreshService, PortWindStationRefreshService>();
builder.Services.AddScoped<IPortWindLatestDataSyncService, PortWindLatestDataSyncService>();
builder.Services.AddScoped<IMetYrForecastRefreshService, MetYrForecastRefreshService>();
builder.Services.AddScoped<IOpenMeteoForecastSupplementService, OpenMeteoForecastSupplementService>();
builder.Services.AddScoped<IWindsMobiSyncService, WindsMobiSyncService>();
builder.Services.AddScoped<ICountryLocatorService, CountryLocatorService>();
builder.Services.AddScoped<IStationDataRetentionService, StationDataRetentionService>();

// Register Schedulers (singleton since Worker is singleton)
builder.Services.AddSingleton<CronScheduler<IMetFrostSyncService>>();
builder.Services.AddSingleton<CronScheduler<IPortWindStationRefreshService>>();
builder.Services.AddSingleton<CronScheduler<IPortWindLatestDataSyncService>>();
builder.Services.AddSingleton<CronScheduler<IHolfuySyncService>>();
builder.Services.AddSingleton<CronScheduler<IMetYrForecastRefreshService>>();
builder.Services.AddSingleton<CronScheduler<IOpenMeteoForecastSupplementService>>();
builder.Services.AddSingleton<CronScheduler<IWindsMobiSyncService>>();
builder.Services.AddSingleton<CronScheduler<ICountryLocatorService>>();
builder.Services.AddSingleton<CronScheduler<IStationDataRetentionService>>();

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

// Register Open-Meteo Client
builder.Services.AddOpenMeteoClient(builder.Configuration);

// Register WindsMobi Client
builder.Services.AddWindsMobiClient(builder.Configuration);

// Register PortWind Client
builder.Services.AddPortWindClient(builder.Configuration);

// Register Google Geocoding Client
builder.Services.AddGoogleGeocodingClient(builder.Configuration);

// Register Health Check Services
builder.Services.AddScoped<DatabaseHealthCheck>();
builder.Services.AddScoped<ForecastCacheSchemaHealthCheck>();
builder.Services.AddScoped<MetFrostHealthCheck>();
builder.Services.AddScoped<HolfuyHealthCheck>();
builder.Services.AddScoped<MetYrHealthCheck>();
builder.Services.AddScoped<OpenMeteoHealthCheck>();
builder.Services.AddScoped<PortWindHealthCheck>();

// Register Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["db", "database"])
    .AddCheck<ForecastCacheSchemaHealthCheck>("forecast-cache-schema", tags: ["db", "database", "schema"])
    .AddCheck<MetFrostHealthCheck>("metfrost", tags: ["api", "metfrost"])
    .AddCheck<HolfuyHealthCheck>("holfuy", tags: ["api", "holfuy"])
    .AddCheck<MetYrHealthCheck>("metyr", tags: ["api", "metyr"])
    .AddCheck<OpenMeteoHealthCheck>("openmeteo", tags: ["api", "openmeteo"])
    .AddCheck<PortWindHealthCheck>("portwind", tags: ["api", "portwind"]);

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

try
{
    Log.Information("Starting WindLordApi.Worker application");

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
