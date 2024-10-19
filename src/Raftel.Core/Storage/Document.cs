using Raftel.Core.BaseTypes;
using Raftel.Core.GuardClauses;
using Raftel.Shared.Extensions;
using Raftel.Shared.GuidGenerators;

namespace Raftel.Core.Storage;

public sealed class Document : Entity<Guid>
{
    private Document()
    {
    }

    internal static Document Create(Folder folder, string name, byte[] content)
    {
        return new Document
        {
            Id = SequentialGuidGenerator.Create(),
            Name = Ensure.NotNullOrEmpty(name, nameof(name)),
            Size = content.SizeInFile(),
            CreationDate = DateTime.UtcNow,
            BlobFileId = SequentialGuidGenerator.Create(),
            FolderId = folder.Id
        };
    }

    public string Name { get; private set; }
    public string Size { get; private set; }
    public DateTime CreationDate { get; private set; }
    public Guid BlobFileId { get; private set; }
    public Guid FolderId { get; private set; }

    public string CalculatePointer()
    {
        return BlobFileId.ToString();
    }
}