using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using WindLordApi.Data.Models;

namespace WindLordApi.Data;

public sealed record ForecastCacheModelContract(
    string SchemaName,
    string TableName,
    IReadOnlyDictionary<string, string> Columns,
    IReadOnlyList<string[]> UniqueConstraints)
{
    public static ForecastCacheModelContract Create(ApplicationDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        var entityType = dbContext.Model.FindEntityType(typeof(ForecastCache))
            ?? throw new InvalidOperationException("ForecastCache is not mapped in the EF model.");

        var tableName = entityType.GetTableName()
            ?? throw new InvalidOperationException("ForecastCache does not map to a database table.");

        var schemaName = entityType.GetSchema() ?? dbContext.Model.GetDefaultSchema() ?? "public";
        var table = StoreObjectIdentifier.Table(tableName, schemaName);

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

        return new ForecastCacheModelContract(schemaName, tableName, columns, uniqueConstraints);
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