namespace Raftel.Infrastructure.BlobStorage;

public class AzureBlobStorageOptions
{
    public string ConnectionString { get; set; }
    public string ContainerName { get; set; }
}