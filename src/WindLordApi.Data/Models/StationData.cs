using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WindLordApi.Data.Models;

[Table("station_data")]
public class StationData
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("wind_speed", TypeName = "numeric(5,2)")]
    public decimal WindSpeed { get; set; }

    [Column("wind_gust", TypeName = "numeric(5,2)")]
    public decimal? WindGust { get; set; }

    [Column("wind_min_speed", TypeName = "numeric(5,2)")]
    public decimal? WindMinSpeed { get; set; }

    [Required]
    [Column("direction")]
    public int Direction { get; set; }

    [Column("temperature", TypeName = "numeric(4,1)")]
    public decimal? Temperature { get; set; }

    [Required]
    [Column("updated_at", TypeName = "timestamp with time zone")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("is_compressed")]
    public bool IsCompressed { get; set; } = false;

    [Required]
    [MaxLength(50)]
    [Column("station_id")]
    public string StationId { get; set; } = string.Empty;

    // Navigation property
    [ForeignKey("StationId")]
    public virtual WeatherStation? WeatherStation { get; set; }
}

