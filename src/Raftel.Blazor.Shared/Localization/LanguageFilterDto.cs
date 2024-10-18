using System.Runtime.Serialization;
using Raftel.Blazor.Shared.Grpc.Filters;

namespace Raftel.Blazor.Shared.Localization;

[DataContract]
public class LanguageFilterDto : GridFilter
{
}