using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using WindLordApi.Data;

namespace WindLordApi.Worker.Startup;

/// <summary>
/// Health check that validates the forecast_cache table contract used by WindLordApi.
/// </summary>
public class ForecastCacheSchemaHealthCheck : IHealthCheck
{
    private const string SchemaName = "public";
    private const string TableName = "forecast_cache";
    private const string ForecastCacheAlternateKey = "forecast_cache_location_id_time_key";

    private static readonly ExpectedColumn[] ExpectedColumns =
    [
        new("id", "bigint"),
        new("location_id", "uuid"),
        new("time", "timestamp with time zone"),
        new("temperature", "numeric"),
        new("wind_speed", "numeric"),
        new("wind_direction", "integer"),
        new("wind_gusts", "numeric"),
        new("precipitation", "numeric"),
        new("precipitation_probability", "real"),
        new("pressure_msl", "numeric"),
        new("weather_code", "text"),
        new("is_day", "smallint"),
        new("landing_wind", "numeric"),
        new("landing_gust", "numeric"),
        new("landing_wind_direction", "integer"),
        new("wind_speed_1000hpa", "numeric"),
        new("wind_direction_1000hpa", "integer"),
        new("wind_speed_925hpa", "numeric"),
        new("wind_direction_925hpa", "integer"),
        new("wind_speed_850hpa", "numeric"),
        new("wind_direction_850hpa", "integer"),
        new("wind_speed_700hpa", "numeric"),
        new("wind_direction_700hpa", "integer"),
        new("temperature_1000hpa", "numeric"),
        new("temperature_925hpa", "numeric"),
        new("temperature_850hpa", "numeric"),
        new("temperature_700hpa", "numeric"),
        new("cloud_cover", "integer"),
        new("cloud_cover_low", "integer"),
        new("cloud_cover_mid", "integer"),
        new("cloud_cover_high", "integer"),
        new("cape", "numeric"),
        new("convective_inhibition", "numeric"),
        new("lifted_index", "numeric"),
        new("boundary_layer_height", "numeric"),
        new("freezing_level_height", "numeric"),
        new("geopotential_height_1000hpa", "numeric"),
        new("geopotential_height_925hpa", "numeric"),
        new("geopotential_height_850hpa", "numeric"),
        new("geopotential_height_700hpa", "numeric"),
        new("created_at", "timestamp with time zone"),
        new("updated_at", "timestamp with time zone"),
        new("precipitation_max", "double precision"),
        new("precipitation_min", "double precision"),
        new("is_yr_data", "boolean")
    ];

    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ForecastCacheSchemaHealthCheck> _logger;

    public ForecastCacheSchemaHealthCheck(
        ApplicationDbContext dbContext,
        ILogger<ForecastCacheSchemaHealthCheck> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var shouldCloseConnection = _dbContext.Database.GetDbConnection().State != ConnectionState.Open;

        try
        {
            if (shouldCloseConnection)
            {
                await _dbContext.Database.OpenConnectionAsync(cancellationToken);
            }

            var actualColumns = await GetActualColumnsAsync(cancellationToken);
            if (actualColumns.Count == 0)
            {
                var missingTableMessage = $"Schema contract check failed: table '{SchemaName}.{TableName}' does not exist or has no visible columns.";
                _logger.LogError(missingTableMessage);
                return HealthCheckResult.Unhealthy(missingTableMessage);
            }

            var missingColumns = ExpectedColumns
                .Where(column => !actualColumns.ContainsKey(column.ColumnName))
                .Select(column => column.ColumnName)
                .OrderBy(columnName => columnName)
                .ToArray();

            if (missingColumns.Length > 0)
            {
                var missingColumnsMessage =
                    $"Schema contract check failed: table '{SchemaName}.{TableName}' is missing expected columns: {string.Join(", ", missingColumns)}.";
                _logger.LogError(missingColumnsMessage);
                return HealthCheckResult.Unhealthy(missingColumnsMessage);
            }

            var mismatchedColumns = ExpectedColumns
                .Select(column => new
                {
                    column.ColumnName,
                    column.ExpectedType,
                    ActualType = actualColumns[column.ColumnName].NormalizedType
                })
                .Where(column => !string.Equals(column.ExpectedType, column.ActualType, StringComparison.OrdinalIgnoreCase))
                .Select(column => $"{column.ColumnName} (expected {column.ExpectedType}, actual {column.ActualType})")
                .OrderBy(message => message)
                .ToArray();

            if (mismatchedColumns.Length > 0)
            {
                var mismatchedColumnsMessage =
                    $"Schema contract check failed: table '{SchemaName}.{TableName}' has incompatible column types: {string.Join("; ", mismatchedColumns)}.";
                _logger.LogError(mismatchedColumnsMessage);
                return HealthCheckResult.Unhealthy(mismatchedColumnsMessage);
            }

            var alternateKeyExists = await ConstraintExistsAsync(cancellationToken);
            if (!alternateKeyExists)
            {
                var missingConstraintMessage =
                    $"Schema contract check failed: table '{SchemaName}.{TableName}' is missing required constraint '{ForecastCacheAlternateKey}'.";
                _logger.LogError(missingConstraintMessage);
                return HealthCheckResult.Unhealthy(missingConstraintMessage);
            }

            var successMessage = $"Schema contract check passed for '{SchemaName}.{TableName}' ({ExpectedColumns.Length} expected columns validated).";
            _logger.LogInformation(successMessage);
            return HealthCheckResult.Healthy(successMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Schema contract health check failed with exception");
            return HealthCheckResult.Unhealthy("Schema contract health check failed", ex);
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await _dbContext.Database.CloseConnectionAsync();
            }
        }
    }

    private async Task<Dictionary<string, ActualColumn>> GetActualColumnsAsync(CancellationToken cancellationToken)
    {
        await using var command = _dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = @"
SELECT column_name, data_type, udt_name
FROM information_schema.columns
WHERE table_schema = @schema
  AND table_name = @tableName;";

        AddParameter(command, "@schema", SchemaName);
        AddParameter(command, "@tableName", TableName);

        var columns = new Dictionary<string, ActualColumn>(StringComparer.OrdinalIgnoreCase);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var columnName = reader.GetString(0);
            var dataType = reader.GetString(1);
            var udtName = reader.GetString(2);

            columns[columnName] = new ActualColumn(columnName, NormalizeActualType(dataType, udtName));
        }

        return columns;
    }

    private async Task<bool> ConstraintExistsAsync(CancellationToken cancellationToken)
    {
        await using var command = _dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = @"
SELECT EXISTS (
    SELECT 1
    FROM pg_constraint constraint_definition
    INNER JOIN pg_class table_definition ON table_definition.oid = constraint_definition.conrelid
    INNER JOIN pg_namespace schema_definition ON schema_definition.oid = table_definition.relnamespace
    WHERE schema_definition.nspname = @schema
      AND table_definition.relname = @tableName
      AND constraint_definition.conname = @constraintName
);";

                AddParameter(command, "@schema", SchemaName);
                AddParameter(command, "@tableName", TableName);
                AddParameter(command, "@constraintName", ForecastCacheAlternateKey);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is bool exists && exists;
    }

    private static void AddParameter(IDbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static string NormalizeActualType(string dataType, string udtName)
    {
        var normalizedUdtName = udtName.Trim().ToLowerInvariant();

        return normalizedUdtName switch
        {
            "bool" => "boolean",
            "int2" => "smallint",
            "int4" => "integer",
            "int8" => "bigint",
            "float4" => "real",
            "float8" => "double precision",
            "timestamptz" => "timestamp with time zone",
            "timestamp" => "timestamp without time zone",
            _ => dataType.Trim().ToLowerInvariant()
        };
    }

    private sealed record ExpectedColumn(string ColumnName, string ExpectedType);

    private sealed record ActualColumn(string ColumnName, string NormalizedType);
}