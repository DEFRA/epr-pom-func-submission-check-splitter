using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SubmissionCheckSplitter.Application.Clients;
using SubmissionCheckSplitter.Application.Clients.Interfaces;
using SubmissionCheckSplitter.Application.Extensions;
using SubmissionCheckSplitter.Application.Handlers;
using SubmissionCheckSplitter.Data.Config;
using SubmissionCheckSplitter.Functions;
using SubmissionCheckSplitter.Functions.Extensions;

[assembly: FunctionsStartup(typeof(Startup))]

namespace SubmissionCheckSplitter.Functions;

[ExcludeFromCodeCoverage]
public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        var services = builder.Services;

        services.AddLogging();
        services.AddConfig();
        services.AddApplication();
        services.AddAzureClients();
        services.AddApplicationInsightsTelemetry();

        services.AddHttpClient<ISubmissionApiClient, SubmissionApiClient>((sp, c) =>
        {
            var submissionApiConfig = sp.GetRequiredService<IOptions<SubmissionApiConfig>>().Value;
            c.BaseAddress = new Uri(submissionApiConfig.BaseUrl);
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        services.AddHttpClient<IValidationDataApiClient, ValidationDataApiClient>((sp, c) =>
        {
            var validationDataApiConfig = sp.GetRequiredService<IOptions<ValidationDataApiConfig>>().Value;
            c.BaseAddress = new Uri(validationDataApiConfig.BaseUrl);
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            c.Timeout = TimeSpan.FromSeconds(validationDataApiConfig.Timeout);
        })
        .AddHttpMessageHandler<ValidationDataApiAuthorisationHandler>();
    }
}