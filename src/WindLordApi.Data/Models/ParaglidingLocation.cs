using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WindLordApi.Data.Models;

[Table("all_paragliding_locations")]
public class ParaglidingLocation
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("longitude")]
    public float Longitude { get; set; }

    [Required]
    [Column("latitude")]
    public float Latitude { get; set; }

    [Required]
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Required]
    [Column("is_main")]
    public bool IsMain { get; set; } = false;

    // Landing location
    [Column("landing_latitude")]
    public float? LandingLatitude { get; set; }

    [Column("landing_longitude")]
    public float? LandingLongitude { get; set; }

    // Navigation property
    public virtual ICollection<ForecastCache> ForecastCaches { get; set; } = new List<ForecastCache>();
}

