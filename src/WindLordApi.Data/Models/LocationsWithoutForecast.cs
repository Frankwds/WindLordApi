using System.ComponentModel.DataAnnotations.Schema;

namespace WindLordApi.Data.Models;

/// <summary>
/// View model for active main locations that don't have any forecasts yet.
/// Represents the locations_without_forecast database view.
/// </summary>
[Table("locations_without_forecast")]
public class LocationsWithoutForecast
{
    [Column("location_id")]
    public Guid LocationId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("latitude")]
    public float Latitude { get; set; }

    [Column("longitude")]
    public float Longitude { get; set; }
}

