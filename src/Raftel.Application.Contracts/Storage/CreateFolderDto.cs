namespace Raftel.Application.Contracts.Storage;

[DataContract]
public class CreateFolderDto
{
    [DataMember(Order = 1)] public string Name { get; set; }
}