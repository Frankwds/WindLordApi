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

    // Atmospheric conditions - Wind at different pressure levels
    [Column("wind_speed_1000hpa", TypeName = "numeric(4,1)")]
    public decimal? WindSpeed1000hpa { get; set; }

    [Column("wind_direction_1000hpa")]
    public int? WindDirection1000hpa { get; set; }

    [Column("wind_speed_925hpa", TypeName = "numeric(4,1)")]
    public decimal? WindSpeed925hpa { get; set; }

    [Column("wind_direction_925hpa")]
    public int? WindDirection925hpa { get; set; }

    [Column("wind_speed_850hpa", TypeName = "numeric(4,1)")]
    public decimal? WindSpeed850hpa { get; set; }

    [Column("wind_direction_850hpa")]
    public int? WindDirection850hpa { get; set; }

    [Column("wind_speed_700hpa", TypeName = "numeric(4,1)")]
    public decimal? WindSpeed700hpa { get; set; }

    [Column("wind_direction_700hpa")]
    public int? WindDirection700hpa { get; set; }

    // Atmospheric conditions - Temperature at different pressure levels
    [Column("temperature_1000hpa", TypeName = "numeric(4,1)")]
    public decimal? Temperature1000hpa { get; set; }

    [Column("temperature_925hpa", TypeName = "numeric(4,1)")]
    public decimal? Temperature925hpa { get; set; }

    [Column("temperature_850hpa", TypeName = "numeric(4,1)")]
    public decimal? Temperature850hpa { get; set; }

    [Column("temperature_700hpa", TypeName = "numeric(4,1)")]
    public decimal? Temperature700hpa { get; set; }

    // Atmospheric conditions - Cloud cover
    [Column("cloud_cover")]
    public int? CloudCover { get; set; }

    [Column("cloud_cover_low")]
    public int? CloudCoverLow { get; set; }

    [Column("cloud_cover_mid")]
    public int? CloudCoverMid { get; set; }

    [Column("cloud_cover_high")]
    public int? CloudCoverHigh { get; set; }

    // Atmospheric conditions - Stability and convection
    [Column("cape", TypeName = "numeric(6,1)")]
    public decimal? Cape { get; set; }

    [Column("convective_inhibition", TypeName = "numeric(6,1)")]
    public decimal? ConvectiveInhibition { get; set; }

    [Column("lifted_index", TypeName = "numeric(4,1)")]
    public decimal? LiftedIndex { get; set; }

    [Column("boundary_layer_height", TypeName = "numeric(6,1)")]
    public decimal? BoundaryLayerHeight { get; set; }

    [Column("freezing_level_height", TypeName = "numeric(6,1)")]
    public decimal? FreezingLevelHeight { get; set; }

    // Atmospheric conditions - Geopotential heights
    [Column("geopotential_height_1000hpa", TypeName = "numeric(6,1)")]
    public decimal? GeopotentialHeight1000hpa { get; set; }

    [Column("geopotential_height_925hpa", TypeName = "numeric(6,1)")]
    public decimal? GeopotentialHeight925hpa { get; set; }

    [Column("geopotential_height_850hpa", TypeName = "numeric(6,1)")]
    public decimal? GeopotentialHeight850hpa { get; set; }

    [Column("geopotential_height_700hpa", TypeName = "numeric(6,1)")]
    public decimal? GeopotentialHeight700hpa { get; set; }

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

