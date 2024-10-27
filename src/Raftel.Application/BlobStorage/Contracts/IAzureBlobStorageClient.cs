namespace Raftel.Application.BlobStorage.Contracts;

public interface IAzureBlobStorageClient
{
    Task UploadAsync(string blobName, byte[] content);
    Task<bool> BlobExistsAsync(string blobName);
    Task DeleteAsync(string blobName);
}