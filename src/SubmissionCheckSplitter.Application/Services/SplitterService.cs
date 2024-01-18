namespace SubmissionCheckSplitter.Application.Services;

using Clients;
using Clients.Interfaces;
using Constants;
using Data.Config;
using Data.Enums;
using Data.Models;
using Data.Models.QueueMessages;
using Data.Models.SubmissionApi;
using Data.Models.ValidationDataApi;
using Exceptions;
using Helpers;
using Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Providers;
using Readers;

public class SplitterService : ISplitterService
{
    private readonly IDequeueProvider _dequeueProvider;
    private readonly IBlobReader _blobReader;
    private readonly ICsvStreamParser _csvStreamParser;
    private readonly IServiceBusQueueClient _serviceBusQueueClient;
    private readonly ISubmissionApiClient _submissionApiClient;
    private readonly IValidationDataApiClient _validationDataApiClient;
    private readonly IIssueCountService _issueCountService;
    private readonly ILogger<SplitterService> _logger;
    private List<CheckSplitterWarning> _warnings = new();
    private int _remainingWarningCount;

    public SplitterService(
        IDequeueProvider dequeueProvider,
        IBlobReader blobReader,
        ICsvStreamParser csvStreamParser,
        IServiceBusQueueClient serviceBusQueueClient,
        ISubmissionApiClient submissionApiClient,
        IValidationDataApiClient validationDataApiClient,
        IIssueCountService issueCountService,
        ILogger<SplitterService> logger)
    {
        _dequeueProvider = dequeueProvider;
        _blobReader = blobReader;
        _csvStreamParser = csvStreamParser;
        _serviceBusQueueClient = serviceBusQueueClient;
        _submissionApiClient = submissionApiClient;
        _validationDataApiClient = validationDataApiClient;
        _issueCountService = issueCountService;
        _logger = logger;
    }

    public async Task ProcessServiceBusMessage(string message, IOptions<ValidationDataApiConfig> validationDataApiOptions, IOptions<ValidationConfig> validationOptions)
    {
        var blobQueueMessage = _dequeueProvider.GetMessageFromJson<BlobQueueMessage>(message);
        var blobMemoryStream = _blobReader.DownloadBlobToStream(blobQueueMessage.BlobName);

        List<string> errors = null;
        var numberOfRecords = 0;

        try
        {
            var csvItems = _csvStreamParser.GetItemsFromCsvStream<CsvDataRow>(blobMemoryStream);

            if (csvItems.Any())
            {
                var numberedCsvItems = csvItems.ToNumberedCsvDataRows(blobQueueMessage.SubmissionPeriod);

                var groupedByProducer = numberedCsvItems
                    .GroupBy(g => g.ProducerId)
                    .ToDictionary(g => g.Key, g => g.ToList());
                numberOfRecords = groupedByProducer.Count;

                var result = await CheckOrganisationIds(
                    blobQueueMessage.OrganisationId,
                    groupedByProducer.Keys.ToArray(),
                    validationDataApiOptions.Value);

                if (result is not null && result.IsComplianceScheme)
                {
                    _remainingWarningCount = validationOptions.Value.MaxIssuesToProcess;
                    _warnings = await CheckComplianceSchemeMembers(
                        blobQueueMessage.OrganisationId,
                        blobQueueMessage.ComplianceSchemeId,
                        blobQueueMessage.BlobName,
                        groupedByProducer.Values.ToArray(),
                        validationDataApiOptions.Value);
                }

                foreach (var producerGroup in groupedByProducer)
                {
                    await _serviceBusQueueClient.AddToProducerValidationQueue(
                        producerGroup.Key,
                        blobQueueMessage,
                        producerGroup.Value);
                }
            }
            else
            {
                _logger.LogInformation(
                    "The CSV file for submission ID {submissionId} is empty",
                    blobQueueMessage.SubmissionId);

                errors = new List<string>
                {
                    ErrorCode.CsvFileEmptyErrorCode
                };
            }
        }
        catch (OrganisationNotFoundException exception)
        {
            _logger.LogError(exception, "Organisation does not match");

            errors = new List<string>
            {
                ErrorCode.OrganisationNotFoundErrorCode,
            };
        }
        catch (CsvParseException exception)
        {
            _logger.LogCritical(exception, "An error occurred parsing the CSV file");

            errors = new List<string>
            {
                ErrorCode.CsvParseExceptionErrorCode,
            };
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "An unexpected error occurred processing the message");

            errors = new List<string>
            {
                ErrorCode.UncaughtExceptionErrorCode,
            };
        }

        try
        {
            await _submissionApiClient.SendReport(
                blobQueueMessage.BlobName,
                blobQueueMessage.OrganisationId,
                blobQueueMessage.UserId,
                blobQueueMessage.SubmissionId,
                numberOfRecords,
                _warnings,
                errors);
        }
        catch (SubmissionApiClientException exception)
        {
            _logger.LogError(exception, "An error occurred sending report");
        }
    }

    private static string FormatStoreKey(string blobName, string issueType)
    {
        return $"{blobName}:{issueType}";
    }

    private async Task<OrganisationDataResult> CheckOrganisationIds(
        string userOrganisationId,
        string[] uploadedProducerIds,
        ValidationDataApiConfig validationDataApiConfig)
    {
        if (!validationDataApiConfig.IsEnabled)
        {
            return null;
        }

        var organisation =
            await _validationDataApiClient.GetOrganisation(userOrganisationId);

        if (!organisation.IsComplianceScheme &&
            (organisation.ReferenceNumber != uploadedProducerIds.First() || uploadedProducerIds.Length > 1))
        {
            throw new OrganisationNotFoundException();
        }

        return organisation;
    }

    private async Task<List<CheckSplitterWarning>> CheckComplianceSchemeMembers(
        string userOrganisationId,
        Guid? complianceSchemeId,
        string blobName,
        List<NumberedCsvDataRow>[] uploadedRows,
        ValidationDataApiConfig validationDataApiConfig)
    {
        if (!validationDataApiConfig.IsEnabled)
        {
            return _warnings;
        }

        var warningStoreKey = FormatStoreKey(blobName, IssueType.Warning);

        var organisationMembers =
            await _validationDataApiClient.GetOrganisationMembers(userOrganisationId, complianceSchemeId);

        foreach (var producerRows in uploadedRows.TakeWhile(_ => _remainingWarningCount > 0))
        {
            var complianceSchemeCheck = producerRows
                .FirstOrDefault(x => !organisationMembers.MemberOrganisations.Contains(x.ProducerId));

            if (complianceSchemeCheck == null)
            {
                continue;
            }

            AddWarningAndUpdateCount(producerRows, blobName);
        }

        await _issueCountService.IncrementIssueCountAsync(warningStoreKey, _warnings.Count);
        return _warnings;
    }

    private async Task AddWarningAndUpdateCount(List<NumberedCsvDataRow> producerRows, string blobName)
    {
        var firstProducerRow = producerRows.First();
        var warningEventRequest = new CheckSplitterWarning(
            EventType.CheckSplitter,
            firstProducerRow.RowNumber,
            blobName,
            new List<string> { ErrorCode.ComplianceSchemeMemberNotFoundErrorCode },
            firstProducerRow.ProducerId,
            firstProducerRow.ProducerType,
            firstProducerRow.ProducerSize,
            firstProducerRow.WasteType,
            firstProducerRow.SubsidiaryId,
            firstProducerRow.DataSubmissionPeriod,
            firstProducerRow.PackagingCategory,
            firstProducerRow.MaterialType,
            firstProducerRow.MaterialSubType,
            firstProducerRow.FromHomeNation,
            firstProducerRow.ToHomeNation,
            firstProducerRow.QuantityKg,
            firstProducerRow.QuantityUnits);

        _warnings.Add(warningEventRequest);

        _remainingWarningCount -= 1;
    }
}