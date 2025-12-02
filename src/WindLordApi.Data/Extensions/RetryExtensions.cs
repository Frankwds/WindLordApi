using Npgsql;

namespace WindLordApi.Data.Extensions;

/// <summary>
/// Extension methods for retry logic and error handling.
/// </summary>
public static class RetryExtensions
{
    /// <summary>
    /// Determines if an exception represents a transient error that should be retried.
    /// Checks for timeout exceptions, Npgsql connection issues, and recursively checks inner exceptions.
    /// </summary>
    /// <param name="ex">The exception to check.</param>
    /// <returns>True if the exception is retryable, false otherwise.</returns>
    public static bool IsRetryableError(Exception ex)
    {
        // Check for timeout exceptions
        if (ex is TimeoutException)
            return true;

        // Check for Npgsql exceptions (connection issues, timeouts)
        if (ex is NpgsqlException npgsqlEx)
        {
            // Check for timeout-related errors
            if (npgsqlEx.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                npgsqlEx.Message.Contains("Timeout", StringComparison.OrdinalIgnoreCase))
                return true;

            // Check for connection-related SQL states (08xxx = connection exceptions)
            if (npgsqlEx.SqlState?.StartsWith("08") == true)
                return true;
        }

        // Check inner exceptions
        if (ex.InnerException != null)
            return IsRetryableError(ex.InnerException);

        return false;
    }
}

