using System.ComponentModel.DataAnnotations.Schema;

namespace WindLordApi.Data.Models;

/// <summary>
/// View model for locations with their oldest forecast update time.
/// Represents the locations_with_oldest_forecast database view.
/// </summary>
[Table("locations_with_oldest_forecast")]
public class LocationsWithOldestForecast
{
    [Column("location_id")]
    public Guid LocationId { get; set; }

    [Column("oldest_updated_at", TypeName = "timestamp with time zone")]
    public DateTime? OldestUpdatedAt { get; set; }
}

