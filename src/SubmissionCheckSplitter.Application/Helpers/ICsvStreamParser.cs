namespace SubmissionCheckSplitter.Application.Helpers;

public interface ICsvStreamParser
{
    IList<T> GetItemsFromCsvStream<T>(MemoryStream memoryStream);
}