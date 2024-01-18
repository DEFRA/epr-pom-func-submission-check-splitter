namespace SubmissionCheckSplitter.Data.Models.SubmissionApi;

using Enums;

public record SubmissionEventRequest(
    int DataCount,
    string BlobName,
    string BlobContainerName,
    List<CheckSplitterWarning> ValidationWarnings = null,
    List<string> Errors = null,
    int Type = (int)EventType.CheckSplitter);