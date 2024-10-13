using Raftel.Application.Contracts.Localization;
using Raftel.Blazor.Shared.Grpc;

namespace Raftel.Blazor.Shared.Localization;

public interface ILanguageService : ICrudService<LanguageDto, CreateLanguageDto, LanguageDto, LanguageFilterDto>
{
}