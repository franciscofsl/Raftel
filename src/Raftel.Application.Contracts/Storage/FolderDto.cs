namespace Raftel.Application.Contracts.Storage;

[DataContract]
public class FolderDto
{
    [DataMember(Order = 1)] public Guid Id { get; set; }
    [DataMember(Order = 2)] public string Name { get; set; }
    public bool IsSelected { get; set; }
}