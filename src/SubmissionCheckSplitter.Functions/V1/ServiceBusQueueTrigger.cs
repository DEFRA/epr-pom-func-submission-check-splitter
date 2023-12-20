﻿namespace SubmissionCheckSplitter.Functions.V1;

using Application.Extensions;
using Application.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SubmissionCheckSplitter.Data.Config;

public class ServiceBusQueueTrigger
{
    private readonly ISplitterService _splitterService;
    private readonly ILogger<ServiceBusQueueTrigger> _logger;
    private readonly IOptions<ValidationDataApiConfig> _validationDataApiOptions;

    public ServiceBusQueueTrigger(ISplitterService splitterService, ILogger<ServiceBusQueueTrigger> logger, IOptions<ValidationDataApiConfig> validationDataApiOptions)
    {
        _splitterService = splitterService;
        _logger = logger;
        _validationDataApiOptions = validationDataApiOptions;
    }

    [FunctionName("ServiceBusQueueTrigger")]
    public async Task RunAsync([ServiceBusTrigger("%ServiceBus:UploadQueueName%", Connection = "ServiceBus:ConnectionString")] string message)
    {
        _logger.LogEnter();

        await _splitterService.ProcessServiceBusMessage(message, _validationDataApiOptions);

        _logger.LogExit();
    }
}