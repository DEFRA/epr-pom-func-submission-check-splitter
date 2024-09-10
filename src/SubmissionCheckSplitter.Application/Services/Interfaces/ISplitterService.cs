namespace SubmissionCheckSplitter.Application.Services;

using Microsoft.Extensions.Options;
using SubmissionCheckSplitter.Data.Config;

public interface ISplitterService
{
    Task ProcessServiceBusMessage(string message, IOptions<ValidationDataApiConfig> validationDataApiOptions, IOptions<ValidationConfig> validationOptions, IOptions<CsvDataFileConfig> csvDataFileConfigOptions);
}