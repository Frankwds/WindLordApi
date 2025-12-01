using WindLordApi.Worker;
using WindLordApi.Worker.Services;
using WindLordApi.Data;
using WindLordApi.Data.Services;
using Microsoft.EntityFrameworkCore;
using WindLordApi.Integrations.MetFrost;


var builder = Host.CreateApplicationBuilder(args);

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
    var connectionString = builder.Configuration.GetConnectionString("SUPABASE_CONNECTION_STRING")
        ?? throw new InvalidOperationException("Supabase connection string not found");

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

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
