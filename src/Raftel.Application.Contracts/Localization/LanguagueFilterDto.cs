using System.Runtime.Serialization;

namespace Raftel.Application.Contracts.Localization;

[DataContract]
public class LanguagueFilterDto
{
    [DataMember(Order = 1)] public string Name { get; set; }

    [DataMember(Order = 2)] public string IsoCode { get; set; }
}