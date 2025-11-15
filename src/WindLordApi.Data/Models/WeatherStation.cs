using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WindLordApi.Data.Models;

[Table("weather_stations")]
public class WeatherStation
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("latitude", TypeName = "numeric(10,8)")]
    public decimal Latitude { get; set; }

    [Required]
    [Column("longitude", TypeName = "numeric(11,8)")]
    public decimal Longitude { get; set; }

    [Column("altitude")]
    public int? Altitude { get; set; } = 0;

    [MaxLength(100)]
    [Column("country")]
    public string? Country { get; set; }

    [Required]
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("provider")]
    public string? Provider { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime? UpdatedAt { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("station_id")]
    public string StationId { get; set; } = string.Empty;

    [Required]
    [Column("is_main")]
    public bool IsMain { get; set; } = false;

    // Navigation property
    public virtual ICollection<StationData> StationData { get; set; } = new List<StationData>();
}

