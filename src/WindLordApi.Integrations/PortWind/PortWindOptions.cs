using System.ComponentModel.DataAnnotations;

namespace WindLordApi.Integrations.PortWind;

public class PortWindOptions
{
    public const string SectionName = "PortWind";
    public const string ProviderName = "PortWind";

    [Required(ErrorMessage = "PortWind StationListUrl is required")]
    [Url(ErrorMessage = "PortWind StationListUrl must be a valid URL")]
    public string StationListUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "PortWind ObservationBaseUrl is required")]
    [Url(ErrorMessage = "PortWind ObservationBaseUrl must be a valid URL")]
    public string ObservationBaseUrl { get; set; } = string.Empty;
}