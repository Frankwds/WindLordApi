using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WindLordApi.Data.Models;

[Table("latest_station_data")]
public class LatestStationData
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("station_id")]
    public string StationId { get; set; } = string.Empty;

    [Required]
    [Column("wind_speed", TypeName = "numeric(5,2)")]
    public decimal WindSpeed { get; set; }

    [Column("wind_gust", TypeName = "numeric(5,2)")]
    public decimal? WindGust { get; set; }

    [Required]
    [Column("direction")]
    public int Direction { get; set; }

    [Column("temperature", TypeName = "numeric(4,1)")]
    public decimal? Temperature { get; set; }

    [Required]
    [Column("updated_at", TypeName = "timestamp with time zone")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("wind_min_speed", TypeName = "numeric(5,2)")]
    public decimal? WindMinSpeed { get; set; }
}

