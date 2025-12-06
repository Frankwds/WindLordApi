using System.ComponentModel.DataAnnotations;

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
    [Required(ErrorMessage = "Holfuy ApiKey is required")]
    public string ApiKey { get; set; } = string.Empty;
}

