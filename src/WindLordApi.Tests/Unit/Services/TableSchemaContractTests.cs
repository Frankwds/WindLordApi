using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WindLordApi.Data;
using WindLordApi.Data.Models;
using WindLordApi.Data.Schema;

namespace WindLordApi.Tests.Unit.Services;

public class TableSchemaContractTests
{
    [Fact]
    public void Create_ForForecastCache_ShouldCaptureColumnShapeFromModel()
    {
        using var dbContext = CreateDbContext();

        var contract = TableSchemaContract.Create<ForecastCache>(dbContext);

        contract.SchemaName.Should().Be("public");
        contract.TableName.Should().Be("forecast_cache");
        contract.Columns["is_yr_data"].IsNullable.Should().BeFalse();
        contract.Columns["location_id"].IsNullable.Should().BeFalse();
        contract.Columns["temperature"].StoreType.Should().Be("numeric");
        contract.Columns["temperature"].Precision.Should().Be(4);
        contract.Columns["temperature"].Scale.Should().Be(1);
        contract.Columns["temperature"].IsNullable.Should().BeTrue();
        contract.UniqueConstraints.Should().ContainSingle(columns =>
            columns.SequenceEqual(new[] { "location_id", "time" }));
    }

    [Fact]
    public void Create_ForStationData_ShouldCaptureNullabilityLengthAndPrecisionFromModel()
    {
        using var dbContext = CreateDbContext();

        var contract = TableSchemaContract.Create<StationData>(dbContext);

        contract.SchemaName.Should().Be("public");
        contract.TableName.Should().Be("station_data");
        contract.Columns["station_id"].IsNullable.Should().BeFalse();
        contract.Columns["station_id"].MaxLength.Should().Be(50);
        contract.Columns["wind_speed"].StoreType.Should().Be("numeric");
        contract.Columns["wind_speed"].Precision.Should().Be(5);
        contract.Columns["wind_speed"].Scale.Should().Be(2);
        contract.Columns["wind_speed"].IsNullable.Should().BeFalse();
        contract.UniqueConstraints.Should().ContainSingle(columns =>
            columns.SequenceEqual(new[] { "station_id", "updated_at" }));
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=windlord_tests;Username=test;Password=test")
            .Options;

        return new ApplicationDbContext(options);
    }
}