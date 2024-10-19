using Microsoft.Extensions.Options;
using Raftel.Application.BlobStorage.Contracts;

namespace Raftel.Infrastructure.BlobStorage;

public class AzureBlobStorageClient(IOptions<AzureBlobStorageOptions> options) : IAzureBlobStorageClient
{
}