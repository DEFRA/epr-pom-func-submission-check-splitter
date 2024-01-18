namespace SubmissionCheckSplitter.Application.Extensions;

using System.Diagnostics.CodeAnalysis;
using Clients;
using Clients.Interfaces;
using Data.Config;
using Handlers;
using Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Providers;
using Readers;
using Services;
using Services.Interfaces;
using StackExchange.Redis;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // helpers
        services.AddSingleton<ICsvStreamParser, CsvStreamParser>();

        // services
        var redisOptions = services.BuildServiceProvider()
            .GetRequiredService<IOptions<RedisConfig>>().Value;

        services.AddScoped<ISplitterService, SplitterService>();
        services.AddScoped<IDequeueProvider, DequeueProvider>();
        services.AddScoped<IBlobReader, BlobReader>();
        services.AddScoped<IServiceBusQueueClient, ServiceBusQueueClient>();
        services.AddScoped<ISubmissionApiClient, SubmissionApiClient>();
        services.AddTransient<ValidationDataApiAuthorisationHandler>();
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisOptions.ConnectionString));
        services.AddSingleton<IIssueCountService, IssueCountService>();

        return services;
    }
}