using Raftel.Core.BaseTypes;
using Raftel.Shared.GuidGenerators;

namespace Raftel.Core.Storage;

public sealed class Document : Entity<Guid>
{
    private Document()
    {
    }

    internal static Document Create(Folder folder, string name, string extension, long size, Guid blobFileId)
    {
        return new Document
        {
            Id = SequentialGuidGenerator.Create(),
            Name = name,
            Extension = extension,
            Size = size,
            CreationDate = DateTime.UtcNow,
            BlobFileId = blobFileId,
            FolderId = folder.Id
        };
    }

    public string Name { get; private set; }
    public string Extension { get; private set; }
    public long Size { get; private set; }
    public DateTime CreationDate { get; private set; }
    public Guid BlobFileId { get; private set; }
    public Guid FolderId { get; private set; }
}