using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WindLordApi.Data.Models;

/// <summary>
/// Combined hourly forecast data from OpenMeteo and MetYr APIs for paragliding locations.
/// Database entity for forecast_cache table.
/// </summary>
[Table("forecast_cache")]
public class ForecastCache
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("location_id")]
    public Guid LocationId { get; set; }

    [Required]
    [Column("time", TypeName = "timestamp with time zone")]
    public DateTime Time { get; set; }

    // Surface conditions
    [Column("temperature", TypeName = "numeric(4,1)")]
    public decimal? Temperature { get; set; }

    [Column("wind_speed", TypeName = "numeric(4,1)")]
    public decimal? WindSpeed { get; set; }

    [Column("wind_direction")]
    public int? WindDirection { get; set; }

    [Column("wind_gusts", TypeName = "numeric(4,1)")]
    public decimal? WindGusts { get; set; }

    [Column("precipitation", TypeName = "numeric(5,2)")]
    public decimal? Precipitation { get; set; }

    [Column("precipitation_probability")]
    public float? PrecipitationProbability { get; set; }

    [Column("pressure_msl", TypeName = "numeric(6,1)")]
    public decimal? PressureMsl { get; set; }

    [Column("weather_code")]
    public string? WeatherCode { get; set; }

    [Column("is_day")]
    public short? IsDay { get; set; } // 0 or 1

    // Landing conditions
    [Column("landing_wind", TypeName = "numeric(4,1)")]
    public decimal? LandingWind { get; set; }

    [Column("landing_gust", TypeName = "numeric(4,1)")]
    public decimal? LandingGust { get; set; }

    [Column("landing_wind_direction")]
    public int? LandingWindDirection { get; set; }

    // Additional fields from database
    [Column("created_at", TypeName = "timestamp with time zone")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp with time zone")]
    public DateTime? UpdatedAt { get; set; }

    [Column("precipitation_max")]
    public double? PrecipitationMax { get; set; }

    [Column("precipitation_min")]
    public double? PrecipitationMin { get; set; }

    [Required]
    [Column("is_yr_data")]
    public bool IsYrData { get; set; } = false;

    // Navigation property
    [ForeignKey("LocationId")]
    public virtual ParaglidingLocation? ParaglidingLocation { get; set; }
}

