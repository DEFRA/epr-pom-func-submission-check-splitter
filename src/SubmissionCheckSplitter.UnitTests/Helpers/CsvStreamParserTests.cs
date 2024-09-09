namespace SubmissionCheckSplitter.UnitTests.Helpers;

using System.Globalization;
using Application.Exceptions;
using Application.Helpers;
using AutoFixture;
using AutoFixture.AutoMoq;
using CsvHelper;
using CsvHelper.Configuration;
using Data.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class CsvStreamParserTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new AutoMoqCustomization());
    private readonly CsvStreamParser _systemUnderTest = new();

    private static CsvConfiguration CsvConfiguration => new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = false, // Force the csv builder to interpret the first row as header
    };

    [TestMethod]
    public void GetItemsFromCsvStream_MapsCsvProperties_WhenCorrectHeaderAddedAndCsvPropertiesAreNotEmptyStrings()
    {
        // Arrange
        var expectedHeaders = GetRequiredHeaders(true);
        var items = _fixture
            .Build<CsvDataRow>()
            .CreateMany(2);
        var csvDataRows = items.Prepend(expectedHeaders);

        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        using var csv = new CsvWriter(writer, CsvConfiguration);

        // Arrange
        csv.WriteRecords(csvDataRows);
        writer.Flush();

        // Act
        var result = _systemUnderTest.GetItemsFromCsvStream<CsvDataRow>(memoryStream);

        // Assert
        result.Should().AllSatisfy(x =>
        {
            x.ProducerId.Should().NotBeNullOrWhiteSpace();
            x.ProducerType.Should().NotBeNullOrWhiteSpace();
            x.ProducerSize.Should().NotBeNullOrWhiteSpace();
            x.SubsidiaryId.Should().NotBeNullOrWhiteSpace();
            x.DataSubmissionPeriod.Should().NotBeNullOrWhiteSpace();
            x.WasteType.Should().NotBeNullOrWhiteSpace();
            x.PackagingCategory.Should().NotBeNullOrWhiteSpace();
            x.MaterialType.Should().NotBeNullOrWhiteSpace();
            x.MaterialSubType.Should().NotBeNullOrWhiteSpace();
            x.FromHomeNation.Should().NotBeNullOrWhiteSpace();
            x.ToHomeNation.Should().NotBeNullOrWhiteSpace();
            x.QuantityKg.Should().NotBeNullOrWhiteSpace();
            x.PreviouslyPaidPackagingMaterialUnits.Should().NotBeNullOrWhiteSpace();
        });
    }

    [TestMethod]
    public void GetItemsFromCsvStream_MapsCsvPropertiesToNull_WhenCorrectHeaderAddedAndCsvPropertiesAreEmptyStrings()
    {
        // Arrange
        var expectedHeaders = GetRequiredHeaders(true);
        var items = _fixture
            .Build<CsvDataRow>()
            .With(x => x.ProducerId, string.Empty)
            .With(x => x.ProducerType, string.Empty)
            .With(x => x.ProducerSize, string.Empty)
            .With(x => x.SubsidiaryId, string.Empty)
            .With(x => x.DataSubmissionPeriod, string.Empty)
            .With(x => x.WasteType, string.Empty)
            .With(x => x.PackagingCategory, string.Empty)
            .With(x => x.MaterialType, string.Empty)
            .With(x => x.MaterialSubType, string.Empty)
            .With(x => x.FromHomeNation, string.Empty)
            .With(x => x.ToHomeNation, string.Empty)
            .With(x => x.QuantityKg, string.Empty)
            .With(x => x.QuantityUnits, string.Empty)
            .With(x => x.PreviouslyPaidPackagingMaterialUnits, string.Empty)
            .CreateMany(2);
        var csvDataRows = items.Prepend(expectedHeaders);

        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        using var csv = new CsvWriter(writer, CsvConfiguration);

        // Arrange
        csv.WriteRecords(csvDataRows);
        writer.Flush();

        // Act
        var result = _systemUnderTest.GetItemsFromCsvStream<CsvDataRow>(memoryStream);

        // Assert
        result.Should().AllSatisfy(x =>
        {
            x.ProducerId.Should().BeNull();
            x.ProducerType.Should().BeNull();
            x.ProducerSize.Should().BeNull();
            x.SubsidiaryId.Should().BeNull();
            x.DataSubmissionPeriod.Should().BeNull();
            x.WasteType.Should().BeNull();
            x.PackagingCategory.Should().BeNull();
            x.MaterialType.Should().BeNull();
            x.MaterialSubType.Should().BeNull();
            x.FromHomeNation.Should().BeNull();
            x.ToHomeNation.Should().BeNull();
            x.QuantityKg.Should().BeNull();
            x.QuantityUnits.Should().BeNull();
            x.PreviouslyPaidPackagingMaterialUnits.Should().BeNull();
        });
    }

    [TestMethod]
    public void GetItemsFromCsvStream_MapsCsvProperties_FieldsShouldBeInOrder()
    {
        // Arrange
        var expectedHeaders = GetRequiredHeaders(true);
        var items = new List<CsvDataRow>
        {
            GenerateDataRow()
        };
        var csvDataRows = items.Prepend(expectedHeaders);

        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        using var csv = new CsvWriter(writer, CsvConfiguration);

        csv.WriteRecords(csvDataRows);
        writer.Flush();

        // Act
        var result = _systemUnderTest.GetItemsFromCsvStream<CsvDataRow>(memoryStream);

        // Assert
        result.Should().AllSatisfy(x =>
        {
            x.ProducerId.Should().Be(items.First().ProducerId);
            x.SubsidiaryId.Should().Be(items.First().SubsidiaryId);
            x.ProducerSize.Should().Be(items.First().ProducerSize);
            x.DataSubmissionPeriod.Should().Be(items.First().DataSubmissionPeriod);
            x.ProducerType.Should().Be(items.First().ProducerType);
            x.WasteType.Should().Be(items.First().WasteType);
            x.PackagingCategory.Should().Be(items.First().PackagingCategory);
            x.MaterialType.Should().Be(items.First().MaterialType);
            x.MaterialSubType.Should().Be(items.First().MaterialSubType);
            x.FromHomeNation.Should().Be(items.First().FromHomeNation);
            x.ToHomeNation.Should().Be(items.First().ToHomeNation);
            x.QuantityKg.Should().Be(items.First().QuantityKg);
            x.QuantityUnits.Should().Be(items.First().QuantityUnits);
            x.PreviouslyPaidPackagingMaterialUnits.Should().Be(items.First().PreviouslyPaidPackagingMaterialUnits);
        });
    }

    [TestMethod]
    public void GetItemsFromCsvStream_DoesNotMapCsvProperties_WhenNoHeaderAdded()
    {
        // Arrange
        var csvDataRows = new List<CsvDataRow>
        {
            GenerateDataRow()
        };

        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        using var csv = new CsvWriter(writer, CsvConfiguration);

        csv.WriteRecords(csvDataRows);
        writer.Flush();

        // Act / Assert
        Assert.ThrowsException<CsvParseException>(() => _systemUnderTest.GetItemsFromCsvStream<CsvDataRow>(memoryStream));
    }

    [TestMethod]
    public void GetItemsFromCsvStream_DoesNotMapCsvProperties_WhenIncorrectHeaderAdded()
    {
        // Arrange
        var incorrectHeader = GetRequiredHeaders(false);
        var items = new List<CsvDataRow>
        {
            GenerateDataRow()
        };
        var csvDataRows = items.Prepend(incorrectHeader);

        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        using var csv = new CsvWriter(writer, CsvConfiguration);

        csv.WriteRecords(csvDataRows);
        writer.Flush();

        // Act / Assert
        Assert.ThrowsException<CsvParseException>(() => _systemUnderTest.GetItemsFromCsvStream<CsvDataRow>(memoryStream));
    }

    [TestMethod]
    public void GetItemsFromCsvStream_DoesNotMapCsvProperties_WhenHeaderIsInIncorrectOrder()
    {
        // Arrange
        var incorrectHeader = new CsvDataRow
        {
            ProducerId = "subsidiary_id",
            SubsidiaryId = "organisation_id",
            ProducerSize = "submission_period",
            DataSubmissionPeriod = "organisation_size",
            ProducerType = "packaging_activity",
            WasteType = "packaging_type",
            PackagingCategory = "packaging_class",
            MaterialType = "packaging_material",
            MaterialSubType = "packaging_material_subtype",
            FromHomeNation = "from_country",
            ToHomeNation = "to_country",
            QuantityKg = "packaging_material_weight",
            QuantityUnits = "packaging_material_units",
            PreviouslyPaidPackagingMaterialUnits = "previously_paid_packaging_material_units"
        };
        var items = new List<CsvDataRow>
        {
            GenerateDataRow()
        };
        var csvDataRows = items.Prepend(incorrectHeader);

        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        using var csv = new CsvWriter(writer, CsvConfiguration);

        csv.WriteRecords(csvDataRows);
        writer.Flush();

        // Act / Assert
        Assert.ThrowsException<CsvParseException>(() => _systemUnderTest.GetItemsFromCsvStream<CsvDataRow>(memoryStream));
    }

    private static CsvDataRow GetRequiredHeaders(bool isValidHeader)
    {
        return new CsvDataRow
        {
            ProducerId = isValidHeader ? "organisation_id" : "random-data",
            SubsidiaryId = "subsidiary_id",
            ProducerSize = "organisation_size",
            DataSubmissionPeriod = "submission_period",
            ProducerType = "packaging_activity",
            WasteType = "packaging_type",
            PackagingCategory = "packaging_class",
            MaterialType = "packaging_material",
            MaterialSubType = "packaging_material_subtype",
            FromHomeNation = "from_country",
            ToHomeNation = "to_country",
            QuantityKg = "packaging_material_weight",
            QuantityUnits = "packaging_material_units",
            PreviouslyPaidPackagingMaterialUnits = "previously_paid_packaging_material_units"
        };
    }

    private static CsvDataRow GenerateDataRow()
    {
        return new CsvDataRow
        {
            ProducerId = "100000",
            SubsidiaryId = "jjHF47",
            ProducerSize = "L",
            DataSubmissionPeriod = "2023-P1",
            ProducerType = "SO",
            WasteType = "CW",
            PackagingCategory = "P1",
            MaterialType = "PL",
            MaterialSubType = "WOOD",
            FromHomeNation = "UK",
            ToHomeNation = "SC",
            QuantityKg = "1234",
            QuantityUnits = "1000",
            PreviouslyPaidPackagingMaterialUnits = "100"
        };
    }
}