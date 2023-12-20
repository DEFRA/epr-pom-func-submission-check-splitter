namespace SubmissionCheckSplitter.Data.Models.SubmissionApi;

public record ValidationReportRow(int RowNumber, List<string> ErrorCodes);