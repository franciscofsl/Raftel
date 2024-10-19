using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using Raftel.Application.BlobStorage.Contracts;

namespace Raftel.Infrastructure.BlobStorage;

public class AzureBlobStorageClient : IAzureBlobStorageClient
{
    private readonly BlobContainerClient _blobContainerClient;

    public AzureBlobStorageClient(IOptions<AzureBlobStorageOptions> options)
    {
        var blobServiceClient = new BlobServiceClient(options.Value.ConnectionString);
        _blobContainerClient = blobServiceClient.GetBlobContainerClient(options.Value.ContainerName);
        _blobContainerClient.CreateIfNotExists();
    }

    public Task UploadAsync(string blobName, byte[] content)
    {
        using var stream = new MemoryStream(content);
        return _blobContainerClient.UploadBlobAsync(blobName, stream);
    }

    public async Task<bool> BlobExistsAsync(string blobName)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobName);
        return await blobClient.ExistsAsync();
    }
}