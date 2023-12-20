namespace SubmissionCheckSplitter.Application.Extensions;

using System.Diagnostics.CodeAnalysis;
using Clients;
using Helpers;
using Microsoft.Extensions.DependencyInjection;
using Providers;
using Readers;
using Services;
using SubmissionCheckSplitter.Application.Clients.Interfaces;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // helpers
        services.AddSingleton<ICsvStreamParser, CsvStreamParser>();

        // services
        services.AddScoped<ISplitterService, SplitterService>();
        services.AddScoped<IDequeueProvider, DequeueProvider>();
        services.AddScoped<IBlobReader, BlobReader>();
        services.AddScoped<IServiceBusQueueClient, ServiceBusQueueClient>();
        services.AddScoped<ISubmissionApiClient, SubmissionApiClient>();
        services.AddScoped<IValidationDataApiClient, ValidationDataApiClient>();

        return services;
    }
}