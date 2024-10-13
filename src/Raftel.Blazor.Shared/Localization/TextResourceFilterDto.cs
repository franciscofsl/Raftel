using System.Runtime.Serialization;
using Raftel.Blazor.Shared.Grpc.Filters;

namespace Raftel.Blazor.Shared.Localization;

[DataContract]
public class TextResourceFilterDto : GridFilter
{
    [DataMember(Order = 1)] public Guid LanguageId { get; set; }
}