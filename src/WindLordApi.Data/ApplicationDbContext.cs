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

            // Check constraint: provider cannot be empty if not null
            entity.ToTable(t => t.HasCheckConstraint("check_provider_not_empty",
                "provider IS NOT NULL AND provider <> ''"));

            // Default values
            entity.Property(e => e.Altitude)
                .HasDefaultValue(0);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            entity.Property(e => e.IsMain)
                .HasDefaultValue(false);

            entity.HasMany(e => e.StationData)
                .WithOne(e => e.WeatherStation)
                .HasPrincipalKey(e => e.StationId)
                .HasForeignKey(e => e.StationId)
                .HasConstraintName("fk_station_data_station_id")
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure StationData
        modelBuilder.Entity<StationData>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.StationId, e.UpdatedAt })
                .IsUnique()
                .HasDatabaseName("unique_station_timestamp");

            // Check constraint: direction must be between 0 and 360
            entity.ToTable(t => t.HasCheckConstraint("station_data_direction_check",
                "direction >= 0 AND direction <= 360"));

            // Default values
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()");

            entity.Property(e => e.IsCompressed)
                .HasDefaultValue(false);
        });
    }
}
