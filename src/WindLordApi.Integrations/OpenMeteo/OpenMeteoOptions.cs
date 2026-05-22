using System.ComponentModel.DataAnnotations;

namespace WindLordApi.Integrations.OpenMeteo;

/// <summary>
/// Configuration options for the Open-Meteo forecast integration.
/// </summary>
public class OpenMeteoOptions
{
    public const string SectionName = "OpenMeteo";
    public const string DefaultBaseUrl = "https://api.open-meteo.com/v1/forecast";

    /// <summary>
    /// Base URL for the Open-Meteo forecast endpoint.
    /// </summary>
    [Required]
    [Url]
    public string BaseUrl { get; set; } = DefaultBaseUrl;
}