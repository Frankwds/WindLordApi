namespace WindLordApi.Integrations.OpenMeteo;

/// <summary>
/// Helpers for normalizing Open-Meteo request and response coordinates.
/// </summary>
public static class OpenMeteoCoordinates
{
    public static double TruncateToRequestPrecision(double value)
    {
        return Math.Truncate(value * 1000d) / 1000d;
    }

    public static bool MatchesRequestPrecision(double left, double right)
    {
        return TruncateToRequestPrecision(left) == TruncateToRequestPrecision(right);
    }
}