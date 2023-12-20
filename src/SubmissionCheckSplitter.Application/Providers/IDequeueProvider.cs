namespace SubmissionCheckSplitter.Application.Providers;

public interface IDequeueProvider
{
    public T GetMessageFromJson<T>(string message);
}