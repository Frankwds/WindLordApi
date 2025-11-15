using Microsoft.EntityFrameworkCore;
using WindLordApi.Data.Models;

namespace WindLordApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<WeatherStation> WeatherStations { get; set; }
    public DbSet<StationData> StationData { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure WeatherStation
        modelBuilder.Entity<WeatherStation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.StationId)
                .IsUnique()
                .HasDatabaseName("weather_stations_station_id_unique");

            entity.HasMany(e => e.StationData)
                .WithOne(e => e.WeatherStation)
                .HasPrincipalKey(e => e.StationId)
                .HasForeignKey(e => e.StationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure StationData
        modelBuilder.Entity<StationData>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.StationId, e.UpdatedAt })
                .IsUnique()
                .HasDatabaseName("unique_station_timestamp");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()");

            entity.Property(e => e.IsCompressed)
                .HasDefaultValue(false);
        });
    }
}
