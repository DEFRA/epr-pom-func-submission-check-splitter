using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Polly.Timeout;
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
        })
        .AddResilienceHandler("SubmissionApiResiliencePipeline", BuildResiliencePipeline());

        services.AddHttpClient<IValidationDataApiClient, ValidationDataApiClient>((sp, c) =>
        {
            var validationDataApiConfig = sp.GetRequiredService<IOptions<ValidationDataApiConfig>>().Value;
            c.BaseAddress = new Uri(validationDataApiConfig.BaseUrl);
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            c.Timeout = TimeSpan.FromSeconds(validationDataApiConfig.Timeout);
        })
        .AddHttpMessageHandler<ValidationDataApiAuthorisationHandler>()
        .AddResilienceHandler("ValidationDataResiliencePipeline", BuildResiliencePipeline<ValidationDataApiConfig>(o => TimeSpan.FromSeconds(o.Timeout)));
    }

    private static Action<ResiliencePipelineBuilder<HttpResponseMessage>> BuildResiliencePipeline() =>
            builder => BuildResiliencePipeline(builder);

    private static Action<ResiliencePipelineBuilder<HttpResponseMessage>, ResilienceHandlerContext> BuildResiliencePipeline<TConfig>(Func<TConfig, TimeSpan> timeoutSelector)
        where TConfig : class =>
        (builder, context) =>
        {
            var sp = context.ServiceProvider;
            var timeout = timeoutSelector(sp.GetRequiredService<IOptions<TConfig>>()?.Value);
            BuildResiliencePipeline(builder, timeout);
        };

    private static void BuildResiliencePipeline(ResiliencePipelineBuilder<HttpResponseMessage> builder, TimeSpan? timeout = null)
    {
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 4,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = (RetryPredicateArguments<HttpResponseMessage> args) =>
            {
                bool shouldHandle;
                var exception = args.Outcome.Exception;
                if (exception is TimeoutRejectedException ||
                   (exception is OperationCanceledException && exception.Source == "System.Private.CoreLib" && exception.InnerException is TimeoutException))
                {
                    shouldHandle = true;
                }
                else
                {
                    shouldHandle = HttpClientResiliencePredicates.IsTransient(args.Outcome);
                }

                return new ValueTask<bool>(shouldHandle);
            },
        });

        if (timeout is not null)
        {
            builder.AddTimeout(timeout.Value);
        }
    }
}