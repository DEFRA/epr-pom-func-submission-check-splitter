namespace SubmissionCheckSplitter.Application.Readers;

public interface IBlobReader
{
    MemoryStream DownloadBlobToStream(string name);
}