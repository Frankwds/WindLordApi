namespace WindLordApi.Worker.Services;

/// <summary>
/// Service for retaining a bounded history of station observations in <c>station_data</c>.
/// </summary>
public interface IStationDataRetentionService
{
    /// <summary>
    /// Deletes station observations older than the configured retention window.
    /// </summary>
    /// <returns>The number of deleted rows.</returns>
    Task<int> CleanupOldObservationsAsync(CancellationToken cancellationToken = default);
}
