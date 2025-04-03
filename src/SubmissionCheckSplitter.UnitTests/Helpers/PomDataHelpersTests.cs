namespace SubmissionCheckSplitter.UnitTests.Helpers;

using AutoFixture;
using AutoFixture.AutoMoq;
using Data.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SubmissionCheckSplitter.Application.Helpers;
using SubmissionCheckSplitter.Data.Config;

[TestClass]
public class PomDataHelpersTests
{
    private const string SubmissionPeriod = "SubmissionPeriod";
    private readonly IFixture _fixture = new Fixture()
        .Customize(new AutoMoqCustomization());

    private List<CsvDataRow>? _csvData;

    [TestInitialize]
    public void Setup()
    {
        _csvData = _fixture.CreateMany<CsvDataRow>(100).ToList();
    }

    [TestMethod]
    public void Should_add_correct_csv_row_numbers()
    {
        // Act
        var numberedData = _csvData.ToNumberedCsvDataRows(It.IsAny<string>(), new CsvDataFileConfig() { EnableTransitionalPackagingUnitsColumn = false, EnableRecyclabilityRatingColumn = false }).ToList();

        // Assert
        for (var i = 0; i < numberedData.Count; i++)
        {
            var csvRow = _csvData[i];
            var numberedRow = numberedData[i];

            numberedRow.ProducerId.Should().Be(csvRow.ProducerId);
            numberedRow.RowNumber.Should().Be(i + 2);
        }
    }

    [TestMethod]
    public void Should_add_correct_csv_submission_period()
    {
        // Act
        var numberedData = _csvData.ToNumberedCsvDataRows(SubmissionPeriod, new CsvDataFileConfig() { EnableTransitionalPackagingUnitsColumn = false, EnableRecyclabilityRatingColumn = true }).ToList();

        // Assert
        for (var i = 0; i < numberedData.Count; i++)
        {
            var csvRow = _csvData[i];
            var submissionPeriodRow = numberedData[i];

            submissionPeriodRow.ProducerId.Should().Be(csvRow.ProducerId);
            submissionPeriodRow.SubmissionPeriod.Should().Be(SubmissionPeriod);
        }
    }
}