namespace SubmissionCheckSplitter.Application.Clients;

using Azure.Messaging.ServiceBus;
using Data.Config;
using Data.Models;
using Data.Models.QueueMessages;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SubmissionCheckSplitter.Application.Clients.Interfaces;

public class ServiceBusQueueClient : IServiceBusQueueClient
{
    private readonly ServiceBusConfig _config;
    private readonly ServiceBusClient _client;

    public ServiceBusQueueClient(IOptions<ServiceBusConfig> options, ServiceBusClient client)
    {
        _config = options.Value;
        _client = client;
    }

    public async Task AddToProducerValidationQueue(
        string producerId,
        BlobQueueMessage blobQueueMessage,
        List<NumberedCsvDataRow> numberedCsvDataRows)
    {
        var producerQueueMessage = new ProducerQueueMessage(producerId, blobQueueMessage, numberedCsvDataRows);
        var serializedMessage = JsonConvert.SerializeObject(producerQueueMessage);
        var serviceBusMessage = new ServiceBusMessage(serializedMessage);

        var sender = _client.CreateSender(_config.SplitQueueName);
        await sender.SendMessageAsync(serviceBusMessage);
        await sender.CloseAsync();
    }
}