using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace WindLordApi.Data.Schema;

public sealed record TableSchemaContract(
    string SchemaName,
    string TableName,
    IReadOnlyDictionary<string, string> Columns,
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
                StoreType = GetNormalizedStoreType(property, table)
            })
            .Where(column => !string.IsNullOrWhiteSpace(column.ColumnName))
            .ToDictionary(
                column => column.ColumnName!,
                column => column.StoreType,
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

    private static string GetNormalizedStoreType(IReadOnlyProperty property, StoreObjectIdentifier table)
    {
        var storeType = property.GetColumnType(table)
            ?? property.GetColumnType()
            ?? property.GetRelationalTypeMapping().StoreType;

        return NormalizeStoreType(storeType);
    }

    private static string NormalizeStoreType(string storeType)
    {
        var normalizedStoreType = storeType.Trim().ToLowerInvariant();
        var precisionStart = normalizedStoreType.IndexOf('(');

        return precisionStart >= 0
            ? normalizedStoreType[..precisionStart]
            : normalizedStoreType;
    }
}