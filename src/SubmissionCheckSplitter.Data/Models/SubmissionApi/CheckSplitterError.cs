namespace SubmissionCheckSplitter.Data.Models.SubmissionApi;

using Enums;

public class CheckSplitterError : CheckSplitterIssue
{
   public EventType ValidationErrorType { get; } = EventType.CheckSplitter;
}