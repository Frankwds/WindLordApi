using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WindLordApi.Data.Models;

[Table("latest_station_data")]
public class LatestStationData
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("station_id", TypeName = "text")]
    public string StationId { get; set; } = string.Empty;

    [Column("wind_speed", TypeName = "numeric")]
    public decimal? WindSpeed { get; set; }

    [Column("wind_gust", TypeName = "numeric")]
    public decimal? WindGust { get; set; }

    [Column("direction", TypeName = "numeric")]
    public decimal? Direction { get; set; }

    [Column("temperature", TypeName = "numeric")]
    public decimal? Temperature { get; set; }

    [Required]
    [Column("updated_at", TypeName = "timestamp with time zone")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("wind_min_speed", TypeName = "numeric")]
    public decimal? WindMinSpeed { get; set; }
}

