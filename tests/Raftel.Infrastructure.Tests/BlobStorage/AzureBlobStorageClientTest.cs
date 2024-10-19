using FluentAssertions;
using Raftel.Application.BlobStorage.Contracts;
using Raftel.Testing;

namespace Raftel.Infrastructure.Tests.BlobStorage;

public class AzureBlobStorageClientTest : TestBase<RaftelInfrastructureTestApplication>
{
    [Fact]
    public async Task BlobStorageClient_ShouldCreateBlob()
    {
        var blobStorageClient = GetRequiredService<IAzureBlobStorageClient>();
        var blobName = Guid.NewGuid().ToString();
        var content = "Test content for blob storage."u8.ToArray();

        await blobStorageClient.UploadAsync(blobName, content);

        var exists = await blobStorageClient.BlobExistsAsync(blobName);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task BlobStorageClient_ShouldDeleteBlob()
    {
        var blobStorageClient = GetRequiredService<IAzureBlobStorageClient>();
        var blobName = Guid.NewGuid().ToString();
        var content = "Test content for blob storage."u8.ToArray();

        await blobStorageClient.UploadAsync(blobName, content);

        await blobStorageClient.DeleteAsync(blobName);

        var exists = await blobStorageClient.BlobExistsAsync(blobName);
        exists.Should().BeFalse();
    }
}