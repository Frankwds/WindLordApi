using WindLordApi.Worker;
using WindLordApi.Data;
using WindLordApi.Data.Services;
using Microsoft.EntityFrameworkCore;
using WindLordApi.Integrations.MetFrost;


var builder = Host.CreateApplicationBuilder(args);

// Register DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("SUPABASE_CONNECTION_STRING")
        ?? throw new InvalidOperationException("Supabase connection string not found");

    options.UseNpgsql(connectionString);
});

// Register Services
builder.Services.AddScoped<IStationDataService, StationDataService>();

// Register MET Frost Client
// 1. Configure Options (binds from appsettings)
builder.Services.Configure<MetFrostOptions>(
    builder.Configuration.GetSection(MetFrostOptions.SectionName));

// 2. Register HttpClient + Service together (recommended)
builder.Services.AddHttpClient<IMetFrostClient, MetFrostClient>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
