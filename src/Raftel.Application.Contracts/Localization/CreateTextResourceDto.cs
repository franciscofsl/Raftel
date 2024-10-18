using System.Runtime.Serialization;

namespace Raftel.Application.Contracts.Localization;

[DataContract]
public class CreateTextResourceDto
{
    [DataMember(Order = 1)] public string Key { get; set; }
    [DataMember(Order = 2)] public string Value { get; set; }
    [DataMember(Order = 3)] public Guid LanguageId { get; set; }
}