using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace WindLordApi.Data;

public class SupabaseClient : DbContext
{
    private readonly IConfiguration _configuration;

    public SupabaseClient(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = _configuration.GetConnectionString("SUPABASE_CONNECTION_STRING")
                ?? throw new InvalidOperationException("Supabase connection string not found");

            optionsBuilder.UseNpgsql(connectionString);
        }
    }

    // Add your DbSets here as you create models
    // public DbSet<YourModel> YourModels { get; set; }
}