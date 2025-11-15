using WindLordApi.Worker;
using WindLordApi.Data;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// Register DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("SUPABASE_CONNECTION_STRING")
        ?? throw new InvalidOperationException("Supabase connection string not found");

    options.UseNpgsql(connectionString);
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
