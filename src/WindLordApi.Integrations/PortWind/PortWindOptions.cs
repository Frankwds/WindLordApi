using System.ComponentModel.DataAnnotations;

namespace WindLordApi.Integrations.PortWind;

/// <summary>
/// Configuration options for the PortWind integration.
/// </summary>
public class PortWindOptions
{
    public const string SectionName = "PortWind";
    public const string ProviderName = "PortWind";
    public const string DefaultStationCatalogUrl = "https://portwind.no/js/stations.js";
    public const string DefaultLatestDataBaseUrl = "https://portwind.no/api/v1/dbdata.php";

    /// <summary>
    /// URL for the JavaScript-wrapped station catalog.
    /// </summary>
    [Required]
    [Url]
    public string StationCatalogUrl { get; set; } = DefaultStationCatalogUrl;

    /// <summary>
    /// Base URL for the latest station observation endpoint.
    /// </summary>
    [Required]
    [Url]
    public string LatestDataBaseUrl { get; set; } = DefaultLatestDataBaseUrl;
}