namespace WindLordApi.Integrations.MetYr;

/// <summary>
/// Configuration options for MET.no Locationforecast API (Yr) client.
/// </summary>
public class MetYrOptions
{
    public const string SectionName = "MetYr";

    /// <summary>
    /// Base URL for the MET.no Locationforecast API.
    /// Defaults to "https://api.met.no/weatherapi/locationforecast/2.0/complete" if not specified.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
}

