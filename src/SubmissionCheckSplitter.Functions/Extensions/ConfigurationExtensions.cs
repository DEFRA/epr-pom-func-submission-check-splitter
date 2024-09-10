namespace SubmissionCheckSplitter.Functions.Extensions;

using System.Diagnostics.CodeAnalysis;
using Data.Config;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SubmissionCheckSplitter.Application.Extensions;

[ExcludeFromCodeCoverage]
public static class ConfigurationExtensions
{
    public static IServiceCollection AddConfig(this IServiceCollection services)
    {
        services.ConfigureSection<CsvDataFileConfig>(CsvDataFileConfig.Section);
        services.ConfigureSection<ServiceBusConfig>(ServiceBusConfig.Section);
        services.ConfigureSection<StorageAccountConfig>(StorageAccountConfig.Section);
        services.ConfigureSection<SubmissionApiConfig>(SubmissionApiConfig.Section);
        services.ConfigureSection<ValidationDataApiConfig>(ValidationDataApiConfig.Section);
        services.ConfigureSection<ValidationConfig>(ValidationConfig.Section);
        services.ConfigureSection<RedisConfig>(RedisConfig.Section);

        return services;
    }

    public static IServiceCollection AddAzureClients(this IServiceCollection services)
    {
        var sp = services.BuildServiceProvider();
        var serviceBusConfig = sp.GetRequiredService<IOptions<ServiceBusConfig>>();

        services.AddAzureClients(clientsBuilder =>
        {
            clientsBuilder.AddServiceBusClient(serviceBusConfig.Value.ConnectionString);
        });
        return services;
    }
}