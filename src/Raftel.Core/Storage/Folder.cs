using Raftel.Core.BaseTypes;
using Raftel.Shared.GuidGenerators;
using Raftel.Shared.Results;

namespace Raftel.Core.Storage;

public sealed class Folder : AggregateRoot<Guid>
{
    private readonly List<Document> _documents = new();
    private readonly List<Folder> _subFolders = new();

    private Folder()
    {
    }

    public static Folder Create(string name)
    {
        return new Folder
        {
            Id = SequentialGuidGenerator.Create(),
            Name = name
        };
    }

    public string Name { get; private set; }

    public IReadOnlyCollection<Folder> SubFolders => _subFolders.AsReadOnly();

    public IReadOnlyCollection<Document> Documents => _documents.AsReadOnly();

    public Result<Folder> CreateFolder(string name)
    {
        if (_subFolders.Any(f => f.Name == name))
        {
            return Result.Failure<Folder>(StorageErrors.DuplicatedFolder);
        }

        var subFolder = Create(name);
        _subFolders.Add(subFolder);
        return Result.Ok(subFolder);
    }

    public void RemoveSubFolder(Folder folder)
    {
        var subFolder = _subFolders.FirstOrDefault(f => f.Id == folder.Id);
        if (subFolder is not null)
        {
            _subFolders.Remove(subFolder);
        }
    }

    public Result<Document> AddDocument(string name, string extension, long size, Guid blobFileId)
    {
        var document = Document.Create(this, name, extension, size, blobFileId);
        _documents.Add(document);
        return Result.Ok(document);
    }

    public void RemoveDocument(Document document)
    {
        var file = _documents.FirstOrDefault(f => f.Id == document.Id);
        if (file is not null)
        {
            _documents.Remove(file);
        }
    }
}