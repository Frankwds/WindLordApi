namespace WindLordApi.Integrations.Holfuy;

/// <summary>
/// Configuration options for Holfuy API client
/// </summary>
public class HolfuyOptions
{
    public const string SectionName = "Holfuy";

    /// <summary>
    /// Holfuy API Key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}

