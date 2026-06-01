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

    private static readonly string[] RequiredUniqueConstraintColumns =
    [
        "location_id",
        "time"
    ];

    private static readonly IReadOnlyDictionary<string, string> ExpectedColumns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "bigint",
        ["location_id"] = "uuid",
        ["time"] = "timestamp with time zone",
        ["temperature"] = "numeric",
        ["wind_speed"] = "numeric",
        ["wind_direction"] = "integer",
        ["wind_gusts"] = "numeric",
        ["precipitation"] = "numeric",
        ["precipitation_probability"] = "real",
        ["pressure_msl"] = "numeric",
        ["weather_code"] = "text",
        ["is_day"] = "smallint",
        ["landing_wind"] = "numeric",
        ["landing_gust"] = "numeric",
        ["landing_wind_direction"] = "integer",
        ["created_at"] = "timestamp with time zone",
        ["updated_at"] = "timestamp with time zone",
        ["precipitation_max"] = "double precision",
        ["precipitation_min"] = "double precision",
        ["is_yr_data"] = "boolean"
    };

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
            var failureMessage = await GetFailureMessageAsync(actualColumns, cancellationToken);

            if (failureMessage is not null)
            {
                return Unhealthy(failureMessage);
            }

            var successMessage = $"Schema contract check passed for '{SchemaName}.{TableName}' ({ExpectedColumns.Count} expected columns validated).";
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

    private async Task<string?> GetFailureMessageAsync(
        IReadOnlyDictionary<string, string> actualColumns,
        CancellationToken cancellationToken)
    {
        if (actualColumns.Count == 0)
        {
            return $"Schema contract check failed: table '{SchemaName}.{TableName}' does not exist or has no visible columns.";
        }

        var missingColumns = ExpectedColumns.Keys
            .Where(columnName => !actualColumns.ContainsKey(columnName))
            .OrderBy(columnName => columnName)
            .ToArray();

        if (missingColumns.Length > 0)
        {
            return $"Schema contract check failed: table '{SchemaName}.{TableName}' is missing expected columns: {string.Join(", ", missingColumns)}.";
        }

        var mismatchedColumns = ExpectedColumns
            .Where(column => !string.Equals(column.Value, actualColumns[column.Key], StringComparison.OrdinalIgnoreCase))
            .Select(column => $"{column.Key} (expected {column.Value}, actual {actualColumns[column.Key]})")
            .OrderBy(message => message)
            .ToArray();

        if (mismatchedColumns.Length > 0)
        {
            return $"Schema contract check failed: table '{SchemaName}.{TableName}' has incompatible column types: {string.Join("; ", mismatchedColumns)}.";
        }

        var uniqueConstraintExists = await ConstraintExistsAsync(cancellationToken);
        if (!uniqueConstraintExists)
        {
            return $"Schema contract check failed: table '{SchemaName}.{TableName}' is missing a unique constraint for columns: {string.Join(", ", RequiredUniqueConstraintColumns)}.";
        }

        return null;
    }

    private HealthCheckResult Unhealthy(string message)
    {
        _logger.LogError(message);
        return HealthCheckResult.Unhealthy(message);
    }

    private async Task<Dictionary<string, string>> GetActualColumnsAsync(CancellationToken cancellationToken)
    {
        await using var command = _dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = @"
SELECT column_name, data_type, udt_name
FROM information_schema.columns
WHERE table_schema = @schema
  AND table_name = @tableName;";

        AddParameter(command, "@schema", SchemaName);
        AddParameter(command, "@tableName", TableName);

        var columns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var columnName = reader.GetString(0);
            var dataType = reader.GetString(1);
            var udtName = reader.GetString(2);

            columns[columnName] = NormalizeActualType(dataType, udtName);
        }

        return columns;
    }

    private async Task<bool> ConstraintExistsAsync(CancellationToken cancellationToken)
    {
        await using var command = _dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = @"
SELECT constraint_name, column_name
FROM information_schema.table_constraints table_constraints
INNER JOIN information_schema.key_column_usage key_column_usage
    ON key_column_usage.constraint_schema = table_constraints.constraint_schema
   AND key_column_usage.constraint_name = table_constraints.constraint_name
WHERE table_constraints.table_schema = @schema
  AND table_constraints.table_name = @tableName
  AND table_constraints.constraint_type = 'UNIQUE';";

                AddParameter(command, "@schema", SchemaName);
                AddParameter(command, "@tableName", TableName);

        var uniqueConstraints = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var constraintName = reader.GetString(0);
            var columnName = reader.GetString(1);

            if (!uniqueConstraints.TryGetValue(constraintName, out var columns))
            {
                columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                uniqueConstraints[constraintName] = columns;
            }

            columns.Add(columnName);
        }

        return uniqueConstraints.Values.Any(HasRequiredUniqueColumns);
    }

    private static bool HasRequiredUniqueColumns(IReadOnlySet<string> columns)
    {
        return columns.Count == RequiredUniqueConstraintColumns.Length
            && RequiredUniqueConstraintColumns.All(columns.Contains);
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

}