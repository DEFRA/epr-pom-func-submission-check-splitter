namespace SubmissionCheckSplitter.Application.Services;

using System.Text.RegularExpressions;
using Clients;
using Clients.Interfaces;
using Constants;
using Data.Config;
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
    private List<CheckSplitterError> _errors = new();
    private int _remainingWarningCount;
    private int _remainingErrorCount;
    private bool _isLatest;

#pragma warning disable S107
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
#pragma warning restore S107

    public async Task ProcessServiceBusMessage(string message, IOptions<ValidationDataApiConfig> validationDataApiOptions, IOptions<ValidationConfig> validationOptions, IOptions<CsvDataFileConfig> csvDataFileConfigOptions)
    {
        var blobQueueMessage = _dequeueProvider.GetMessageFromJson<BlobQueueMessage>(message);
        var blobMemoryStream = _blobReader.DownloadBlobToStream(blobQueueMessage.BlobName);
        _isLatest = csvDataFileConfigOptions.Value.EnableTransitionalPackagingUnitsColumn;

        List<string> errors = null;
        var numberOfRecords = 0;

        try
        {
            var csvItems = _csvStreamParser.GetItemsFromCsvStream<CsvDataRow>(blobMemoryStream, csvDataFileConfigOptions.Value);

            if (csvItems.Any())
            {
                var numberedCsvItems = csvItems.ToNumberedCsvDataRows(blobQueueMessage.SubmissionPeriod, csvDataFileConfigOptions.Value);

                var groupedByProducer = numberedCsvItems
                    .GroupBy(g => g.ProducerId)
                    .ToDictionary(g => g.Key, g => g.ToList());
                numberOfRecords = groupedByProducer.Count;

                var result = await CheckOrganisationIds(
                    blobQueueMessage.OrganisationId,
                    groupedByProducer.Keys.ToArray(),
                    validationDataApiOptions.Value);

                if (result?.IsComplianceScheme == true)
                {
                    _remainingWarningCount = validationOptions.Value.MaxIssuesToProcess;
                    _warnings = await CheckComplianceSchemeMembers(
                        blobQueueMessage.OrganisationId,
                        blobQueueMessage.ComplianceSchemeId,
                        blobQueueMessage.BlobName,
                        groupedByProducer.Values.ToArray(),
                        validationDataApiOptions.Value);

                    _remainingErrorCount = validationOptions.Value.MaxIssuesToProcess;
                    _errors = await CheckComplianceSchemeOrganisationsExist(
                        groupedByProducer,
                        blobQueueMessage.BlobName,
                        validationDataApiOptions.Value);

                    RemoveComplianceSchemeWarnings(_errors.Count, _warnings.Count);
                }

                await ProcessProducerGroups(groupedByProducer, blobQueueMessage);
            }
            else
            {
                _logger.LogInformation(
                    "The CSV file for submission ID {SubmissionId} is empty",
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
        catch (ArgumentNullException exception)
        {
            _logger.LogError(exception, "CSV data rows are invalid OR it is missing Organisation id (Hint check for invisible rows in CSV)");
            errors = new List<string>
            {
                ErrorCode.CsvFileEmptyErrorCode,
            };
        }
        catch (ValidationDataApiClientException exception)
        {
            _logger.LogError(exception, "{Message}", exception.Message);

            errors = new List<string>
            {
                ErrorCode.OrganisationNotFoundErrorCode,
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
                _errors,
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

    private static bool CheckProducerIsValidFormat(KeyValuePair<string, List<NumberedCsvDataRow>> producer)
    {
        var pattern = "^[0-9]{6}$";
        var match = Regex.Match(producer.Key, pattern, RegexOptions.None, TimeSpan.FromSeconds(2));
        return match.Success;
    }

    private T CreateIssueEventRequest<T>(NumberedCsvDataRow firstProducerRow, string blobName, string errorCode)
        where T : CheckSplitterIssue, new()
    {
        var request = new T()
        {
            RowNumber = firstProducerRow.RowNumber,
            BlobName = blobName,
            ErrorCodes = new List<string> { errorCode },
            ProducerId = firstProducerRow.ProducerId,
            ProducerType = firstProducerRow.ProducerType,
            ProducerSize = firstProducerRow.ProducerSize,
            WasteType = firstProducerRow.WasteType,
            SubsidiaryId = firstProducerRow.SubsidiaryId,
            DataSubmissionPeriod = firstProducerRow.DataSubmissionPeriod,
            PackagingCategory = firstProducerRow.PackagingCategory,
            MaterialType = firstProducerRow.MaterialType,
            MaterialSubType = firstProducerRow.MaterialSubType,
            FromHomeNation = firstProducerRow.FromHomeNation,
            ToHomeNation = firstProducerRow.ToHomeNation,
            QuantityKg = firstProducerRow.QuantityKg,
            QuantityUnits = firstProducerRow.QuantityUnits,
            TransitionalPackagingUnits = _isLatest ? firstProducerRow.TransitionalPackagingUnits : null,
            RecyclabilityRating = firstProducerRow.RecyclabilityRating
        };

        return request;
    }

    private void RemoveComplianceSchemeWarnings(int errorCount, int warningCount)
    {
        if (errorCount > 0 && warningCount > 0)
        {
            var existErrors = new HashSet<int>(_errors
                .Where(e => e.ErrorCodes.Contains(ErrorCode.OrganisationDoesNotExistExistErrorCode))
                .Select(e => e.RowNumber));
            var intersectWarnings = _warnings
                .Where(w => existErrors.Contains(w.RowNumber) &&
                            w.ErrorCodes.Contains(ErrorCode.ComplianceSchemeMemberNotFoundErrorCode))
                .ToList();
            _warnings = _warnings.Except(intersectWarnings).ToList();
        }
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
            (organisation.ReferenceNumber != uploadedProducerIds[0] || uploadedProducerIds.Length > 1))
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
                .Find(x => organisationMembers != null && !organisationMembers.MemberOrganisations.Contains(x.ProducerId));

            if (complianceSchemeCheck == null && organisationMembers != null)
            {
                continue;
            }

            await AddIssueAndUpdateCountAsync(producerRows, blobName, ErrorCode.ComplianceSchemeMemberNotFoundErrorCode, IssueType.Warning);
        }

        await _issueCountService.PersistIssueCountToRedisAsync(warningStoreKey, _warnings.Count);
        return _warnings;
    }

    private async Task<List<CheckSplitterError>> CheckComplianceSchemeOrganisationsExist(
        Dictionary<string, List<NumberedCsvDataRow>> uploadedProducers,
        string blobName,
        ValidationDataApiConfig validationDataApiConfig)
    {
        if (!validationDataApiConfig.IsEnabled)
        {
            return _errors;
        }

        var producerList = uploadedProducers.Keys.ToList();
        var errorStoreKey = FormatStoreKey(blobName, IssueType.Error);

        foreach (var producer in uploadedProducers.TakeWhile(_ => _remainingErrorCount > 0))
        {
            var isValidFormat = CheckProducerIsValidFormat(producer);

            if (!isValidFormat)
            {
                producerList.Remove(producer.Key);
            }
        }

        if (_remainingErrorCount > 0)
        {
            var organisations = await _validationDataApiClient.GetValidOrganisations(producerList.ToArray());

            var filteredProducers = uploadedProducers
                .Where(kv => producerList.Contains(kv.Key));

            foreach (var producer in filteredProducers.TakeWhile(_ => _remainingErrorCount > 0))
            {
                if (!organisations.ReferenceNumbers.Contains(producer.Key))
                {
                    await AddIssueAndUpdateCountAsync(producer.Value, blobName, ErrorCode.OrganisationDoesNotExistExistErrorCode, IssueType.Error);
                }
            }
        }

        await _issueCountService.PersistIssueCountToRedisAsync(errorStoreKey, _errors.Count);
        return _errors;
    }

    private async Task AddIssueAndUpdateCountAsync(List<NumberedCsvDataRow> producerRows, string blobName, string errorCode, string issueType)
    {
        var firstProducerRow = producerRows[0];

        if (issueType == IssueType.Error)
        {
            var errorEventRequest = CreateIssueEventRequest<CheckSplitterError>(firstProducerRow, blobName, errorCode);

            _errors.Add(errorEventRequest);
            _remainingErrorCount -= 1;
        }
        else
        {
            var warningEventRequest = CreateIssueEventRequest<CheckSplitterWarning>(firstProducerRow, blobName, errorCode);

            _warnings.Add(warningEventRequest);
            _remainingWarningCount -= 1;
        }
    }

    private async Task ProcessProducerGroups(Dictionary<string, List<NumberedCsvDataRow>> groupedByProducer, BlobQueueMessage blobQueueMessage)
    {
        foreach (var producerGroup in groupedByProducer)
        {
            await _serviceBusQueueClient.AddToProducerValidationQueue(
                producerGroup.Key,
                blobQueueMessage,
                producerGroup.Value);
        }
    }
}