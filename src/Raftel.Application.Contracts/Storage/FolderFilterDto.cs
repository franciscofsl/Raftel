namespace Raftel.Application.Contracts.Storage;

[DataContract]
public class FolderFilterDto
{
    public Guid? ParentFolderId { get; set; }
}