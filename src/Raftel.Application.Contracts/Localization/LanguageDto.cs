using System.Runtime.Serialization;

namespace Raftel.Application.Contracts.Localization;

[DataContract]
public class LanguageDto
{
    [DataMember(Order = 1)] public Guid Id { get; set; }

    [DataMember(Order = 2)] public string Name { get;  set; }

    [DataMember(Order = 3)] public string IsoCode { get; set; }
}