namespace SubmissionCheckSplitter.Application.Services;

using Clients;
using Clients.Interfaces;
using Constants;
using Data.Config;
using Data.Models;
using Data.Models.QueueMessages;
using Exceptions;
using Helpers;
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
    private readonly ILogger<SplitterService> _logger;

    public SplitterService(
        IDequeueProvider dequeueProvider,
        IBlobReader blobReader,
        ICsvStreamParser csvStreamParser,
        IServiceBusQueueClient serviceBusQueueClient,
        ISubmissionApiClient submissionApiClient,
        IValidationDataApiClient validationDataApiClient,
        ILogger<SplitterService> logger)
    {
        _dequeueProvider = dequeueProvider;
        _blobReader = blobReader;
        _csvStreamParser = csvStreamParser;
        _serviceBusQueueClient = serviceBusQueueClient;
        _submissionApiClient = submissionApiClient;
        _validationDataApiClient = validationDataApiClient;
        _logger = logger;
    }

    public async Task ProcessServiceBusMessage(string message, IOptions<ValidationDataApiConfig> validationDataApiOptions)
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

                await CheckOrganisationIds(blobQueueMessage.OrganisationId, groupedByProducer.Keys.ToArray(), validationDataApiOptions.Value);

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
                errors);
        }
        catch (SubmissionApiClientException exception)
        {
            _logger.LogError(exception, "An error occurred sending report");
        }
    }

    private async Task CheckOrganisationIds(string userOrganisationId, string[] uploadedProducerIds, ValidationDataApiConfig validationDataApiConfig)
    {
        if (!validationDataApiConfig.IsEnabled)
        {
            return;
        }

        var organisation =
                await _validationDataApiClient.GetOrganisation(userOrganisationId);

        if (!organisation.IsComplianceScheme &&
                (organisation.ReferenceNumber != uploadedProducerIds.First() || uploadedProducerIds.Length > 1))
            {
                throw new OrganisationNotFoundException();
            }
    }
}