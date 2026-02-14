using System.ComponentModel.DataAnnotations;

namespace WindLordApi.Integrations.GoogleGeocoding;

/// <summary>
/// Options for Google Geocoding API configuration.
/// </summary>
public class GoogleGeocodingOptions
{
    public const string SectionName = "GoogleGeocoding";

    /// <summary>
    /// Google Geocoding API key.
    /// </summary>
    [Required]
    public string ApiKey { get; set; } = string.Empty;
}
