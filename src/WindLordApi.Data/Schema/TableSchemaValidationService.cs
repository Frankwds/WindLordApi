using System.Data;
using Microsoft.EntityFrameworkCore;

namespace WindLordApi.Data.Schema;

public sealed class TableSchemaValidationService
{
    private readonly ApplicationDbContext _dbContext;

    public TableSchemaValidationService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TableSchemaValidationResult> ValidateAsync<TEntity>(CancellationToken cancellationToken = default)
        where TEntity : class
    {
        var contract = TableSchemaContract.Create<TEntity>(_dbContext);
        var shouldCloseConnection = _dbContext.Database.GetDbConnection().State != ConnectionState.Open;

        try
        {
            if (shouldCloseConnection)
            {
                await _dbContext.Database.OpenConnectionAsync(cancellationToken);
            }

            var actualColumns = await GetActualColumnsAsync(contract, cancellationToken);
            var columnFailure = GetColumnFailureMessage(contract, actualColumns);
            if (columnFailure is not null)
            {
                return TableSchemaValidationResult.Invalid(columnFailure);
            }

            var actualUniqueConstraints = await GetActualUniqueConstraintsAsync(contract, cancellationToken);
            var uniqueConstraintFailure = GetUniqueConstraintFailureMessage(contract, actualUniqueConstraints);
            if (uniqueConstraintFailure is not null)
            {
                return TableSchemaValidationResult.Invalid(uniqueConstraintFailure);
            }

            return TableSchemaValidationResult.Valid(
                $"Schema contract check passed for '{contract.SchemaName}.{contract.TableName}' ({contract.Columns.Count} expected columns validated).");
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await _dbContext.Database.CloseConnectionAsync();
            }
        }
    }

    private async Task<Dictionary<string, string>> GetActualColumnsAsync(
        TableSchemaContract contract,
        CancellationToken cancellationToken)
    {
        await using var command = _dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = @"
SELECT column_name, data_type, udt_name
FROM information_schema.columns
WHERE table_schema = @schema
  AND table_name = @tableName;";

        AddParameter(command, "@schema", contract.SchemaName);
        AddParameter(command, "@tableName", contract.TableName);

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

    private static string? GetColumnFailureMessage(
        TableSchemaContract contract,
        IReadOnlyDictionary<string, string> actualColumns)
    {
        if (actualColumns.Count == 0)
        {
            return $"Schema contract check failed: table '{contract.SchemaName}.{contract.TableName}' does not exist or has no visible columns.";
        }

        var missingColumns = contract.Columns.Keys
            .Where(columnName => !actualColumns.ContainsKey(columnName))
            .OrderBy(columnName => columnName)
            .ToArray();

        if (missingColumns.Length > 0)
        {
            return $"Schema contract check failed: table '{contract.SchemaName}.{contract.TableName}' is missing expected columns: {string.Join(", ", missingColumns)}.";
        }

        var mismatchedColumns = contract.Columns
            .Where(column => !string.Equals(column.Value, actualColumns[column.Key], StringComparison.OrdinalIgnoreCase))
            .Select(column => $"{column.Key} (expected {column.Value}, actual {actualColumns[column.Key]})")
            .OrderBy(message => message)
            .ToArray();

        if (mismatchedColumns.Length > 0)
        {
            return $"Schema contract check failed: table '{contract.SchemaName}.{contract.TableName}' has incompatible column types: {string.Join("; ", mismatchedColumns)}.";
        }

        return null;
    }

    private async Task<List<string[]>> GetActualUniqueConstraintsAsync(
        TableSchemaContract contract,
        CancellationToken cancellationToken)
    {
        await using var command = _dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = @"
SELECT table_constraints.constraint_name, key_column_usage.column_name
FROM information_schema.table_constraints table_constraints
INNER JOIN information_schema.key_column_usage key_column_usage
    ON key_column_usage.constraint_schema = table_constraints.constraint_schema
   AND key_column_usage.constraint_name = table_constraints.constraint_name
WHERE table_constraints.table_schema = @schema
  AND table_constraints.table_name = @tableName
  AND table_constraints.constraint_type = 'UNIQUE';";

        AddParameter(command, "@schema", contract.SchemaName);
        AddParameter(command, "@tableName", contract.TableName);

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

        return uniqueConstraints.Values
            .Select(columns => columns
                .OrderBy(columnName => columnName, StringComparer.OrdinalIgnoreCase)
                .ToArray())
            .ToList();
    }

    private static string? GetUniqueConstraintFailureMessage(
        TableSchemaContract contract,
        IReadOnlyList<string[]> actualUniqueConstraints)
    {
        var missingUniqueConstraint = contract.UniqueConstraints
            .FirstOrDefault(requiredColumns => !actualUniqueConstraints.Any(actualColumns => ColumnsMatch(actualColumns, requiredColumns)));

        return missingUniqueConstraint is null
            ? null
            : $"Schema contract check failed: table '{contract.SchemaName}.{contract.TableName}' is missing a unique constraint for columns: {string.Join(", ", missingUniqueConstraint)}.";
    }

    private static bool ColumnsMatch(IReadOnlyList<string> actualColumns, IReadOnlyList<string> requiredColumns)
    {
        return actualColumns.Count == requiredColumns.Count
            && requiredColumns.All(requiredColumn => actualColumns.Contains(requiredColumn, StringComparer.OrdinalIgnoreCase));
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