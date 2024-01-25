namespace SubmissionCheckSplitter.Data.Models.SubmissionApi;

using Enums;

public class CheckSplitterWarning : CheckSplitterIssue
{
   public EventType ValidationWarningType { get; } = EventType.CheckSplitter;
}