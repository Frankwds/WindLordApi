using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace WindLordApi.Data.Schema;

public sealed record ColumnSchemaContract(
    string StoreType,
    bool IsNullable,
    int? MaxLength,
    int? Precision,
    int? Scale);

public sealed record TableSchemaContract(
    string SchemaName,
    string TableName,
    IReadOnlyDictionary<string, ColumnSchemaContract> Columns,
    IReadOnlyList<string[]> UniqueConstraints)
{
    public static TableSchemaContract Create<TEntity>(ApplicationDbContext dbContext)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        var entityType = dbContext.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} is not mapped in the EF model.");

        var tableName = entityType.GetTableName()
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} does not map to a database table.");

        var modelSchemaName = entityType.GetSchema() ?? dbContext.Model.GetDefaultSchema();
        var databaseSchemaName = modelSchemaName ?? "public";
        var table = StoreObjectIdentifier.Table(tableName, modelSchemaName);

        var columns = entityType.GetProperties()
            .Select(property => new
            {
                ColumnName = property.GetColumnName(table),
                ColumnContract = CreateColumnContract(property, table)
            })
            .Where(column => !string.IsNullOrWhiteSpace(column.ColumnName))
            .ToDictionary(
                column => column.ColumnName!,
                column => column.ColumnContract,
                StringComparer.OrdinalIgnoreCase);

        var uniqueConstraints = entityType.GetKeys()
            .Where(key => !key.IsPrimaryKey())
            .Select(key => key.Properties
                .Select(property => property.GetColumnName(table))
                .Where(columnName => !string.IsNullOrWhiteSpace(columnName))
                .OrderBy(columnName => columnName, StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .ToArray())
            .Where(columns => columns.Length > 0)
            .ToArray();

        return new TableSchemaContract(databaseSchemaName, tableName, columns, uniqueConstraints);
    }

    private static ColumnSchemaContract CreateColumnContract(IReadOnlyProperty property, StoreObjectIdentifier table)
    {
        var rawStoreType = GetStoreType(property, table);
        var (storeTypeMaxLength, storeTypePrecision, storeTypeScale) = ParseStoreTypeMetadata(rawStoreType);

        return new ColumnSchemaContract(
            StoreType: NormalizeStoreType(rawStoreType),
            IsNullable: property.IsNullable,
            MaxLength: property.GetMaxLength() ?? storeTypeMaxLength,
            Precision: property.GetPrecision() ?? storeTypePrecision,
            Scale: property.GetScale() ?? storeTypeScale);
    }

    private static string GetStoreType(IReadOnlyProperty property, StoreObjectIdentifier table)
    {
        return property.GetColumnType(table)
            ?? property.GetColumnType()
            ?? property.GetRelationalTypeMapping().StoreType;
    }

    private static string NormalizeStoreType(string storeType)
    {
        var normalizedStoreType = storeType.Trim().ToLowerInvariant();
        var precisionStart = normalizedStoreType.IndexOf('(');

        return precisionStart >= 0
            ? normalizedStoreType[..precisionStart]
            : normalizedStoreType;
    }

    private static (int? MaxLength, int? Precision, int? Scale) ParseStoreTypeMetadata(string storeType)
    {
        var normalizedStoreType = storeType.Trim().ToLowerInvariant();
        var start = normalizedStoreType.IndexOf('(');
        var end = normalizedStoreType.LastIndexOf(')');

        if (start < 0 || end <= start)
        {
            return (null, null, null);
        }

        var values = normalizedStoreType[(start + 1)..end]
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (values.Length == 0 || !int.TryParse(values[0], out var firstValue))
        {
            return (null, null, null);
        }

        return NormalizeStoreType(storeType) switch
        {
            "numeric" or "decimal" =>
                values.Length > 1 && int.TryParse(values[1], out var scale)
                    ? (null, firstValue, scale)
                    : (null, firstValue, null),
            "character varying" or "varchar" or "character" or "char" => (firstValue, null, null),
            _ => (null, null, null)
        };
    }

}