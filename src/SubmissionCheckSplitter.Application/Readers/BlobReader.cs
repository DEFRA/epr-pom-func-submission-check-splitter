namespace SubmissionCheckSplitter.Application.Readers;

using Azure.Storage.Blobs;
using Data.Config;
using Microsoft.Extensions.Options;

public class BlobReader : IBlobReader
{
    private readonly StorageAccountConfig _storageAccountConfig;

    public BlobReader(IOptions<StorageAccountConfig> storageAccountOptions)
    {
        _storageAccountConfig = storageAccountOptions.Value;
    }

    public MemoryStream DownloadBlobToStream(string name)
    {
        var blobClient = GetBlobClient(name);

        var memoryStream = new MemoryStream();
        blobClient.DownloadTo(memoryStream);

        return memoryStream;
    }

    private BlobClient GetBlobClient(string blobName)
    {
        return new BlobClient(_storageAccountConfig.ConnectionString, _storageAccountConfig.PomBlobContainerName, blobName);
    }
}
