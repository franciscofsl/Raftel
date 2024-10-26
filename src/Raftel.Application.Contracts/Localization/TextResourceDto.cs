namespace Raftel.Application.Contracts.Localization;

[DataContract]
public class TextResourceDto
{
    [DataMember(Order = 1)] public Guid Id { get; set; }
    [DataMember(Order = 2)] public string Key { get; set; }
    [DataMember(Order = 3)] public string Value { get; set; }
    [DataMember(Order = 4)] public Guid LanguageId { get; set; }
}