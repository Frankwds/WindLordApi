namespace WindLordApi.Integrations.MetFrost;

/// <summary>
/// Configuration options for MET Frost API client
/// </summary>
public class MetFrostOptions
{
    public const string SectionName = "Met";

    /// <summary>
    /// MET Frost API Client ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// MET Frost API Client Secret (currently not used in Basic auth, but kept for future use)
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;
}

