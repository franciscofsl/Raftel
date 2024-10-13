using Raftel.Application.Contracts.Localization;
using Raftel.Blazor.Shared.Grpc;

namespace Raftel.Blazor.Shared.Localization;

public interface ITextResourceService : ICrudService<TextResourceDto, CreateTextResourceDto, TextResourceDto, TextResourceFilterDto>
{
}