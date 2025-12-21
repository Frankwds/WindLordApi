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

    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [Column("longitude")]
    public float Longitude { get; set; }

    [Required]
    [Column("latitude")]
    public float Latitude { get; set; }

    [Required]
    [Column("altitude")]
    public int Altitude { get; set; } = 0;

    [Required]
    [MaxLength(100)]
    [Column("country")]
    public string Country { get; set; } = "Norway";

    [Required]
    [MaxLength(50)]
    [Column("flightlog_id")]
    public string FlightlogId { get; set; } = string.Empty;

    [Required]
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at", TypeName = "timestamp with time zone")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp with time zone")]
    public DateTime? UpdatedAt { get; set; }

    // Direction flags
    [Required]
    [Column("n")]
    public bool N { get; set; } = false;

    [Required]
    [Column("ne")]
    public bool NE { get; set; } = false;

    [Required]
    [Column("e")]
    public bool E { get; set; } = false;

    [Required]
    [Column("se")]
    public bool SE { get; set; } = false;

    [Required]
    [Column("s")]
    public bool S { get; set; } = false;

    [Required]
    [Column("sw")]
    public bool SW { get; set; } = false;

    [Required]
    [Column("w")]
    public bool W { get; set; } = false;

    [Required]
    [Column("nw")]
    public bool NW { get; set; } = false;

    [Required]
    [Column("is_main")]
    public bool IsMain { get; set; } = false;

    // Landing location
    [Column("landing_latitude")]
    public float? LandingLatitude { get; set; }

    [Column("landing_longitude")]
    public float? LandingLongitude { get; set; }

    [Column("landing_altitude")]
    public int? LandingAltitude { get; set; }

    [Column("timezone")]
    public string? Timezone { get; set; } = string.Empty;

    // Navigation property
    public virtual ICollection<ForecastCache> ForecastCaches { get; set; } = new List<ForecastCache>();
}

