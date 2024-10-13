using System.ServiceModel;
using Raftel.Application.Contracts.Localization;
using Raftel.Application.Cqrs.Commands;
using Raftel.Application.Cqrs.Queries;
using Raftel.Application.Localization.Commands.Create;
using Raftel.Application.Localization.Commands.Delete;
using Raftel.Application.Localization.Commands.Update;
using Raftel.Application.Localization.Queries.ById;
using Raftel.Application.Localization.Queries.GetList;
using Raftel.Blazor.Shared.Grpc.Filters;
using Raftel.Blazor.Shared.Localization;
using Raftel.Core.Localization.ValueObjects;
using Raftel.Server.Grpc;

namespace Raftel.Server.Localization;

[ServiceContract]
public class LanguageService(IQueryDispatcher queryDispatcher, ICommandDispatcher commandDispatcher)
    : CrudService<LanguageDto, CreateLanguageDto, LanguageDto, LanguageFilterDto>(queryDispatcher, commandDispatcher),
        ILanguageService
{
    public override async Task<LanguageDto> CreateAsync(CreateLanguageDto createDto)
    {
        var command = new CreateLanguageCommand
        {
            Name = createDto.Name,
            IsoCode = createDto.IsoCode
        };

        var result = await CommandDispatcher.Dispatch(command);

        return result.Value;
    }

    public override async Task<LanguageDto> UpdateAsync(LanguageDto dto)
    {
        var command = new UpdateLanguageCommand
        {
            Id = dto.Id,
            Name = dto.Name,
        };

        var result = await CommandDispatcher.Dispatch(command);
        return result.Value;
    }

    public override Task DeleteAsync(EntityByIdFilter filter)
    {
        var command = new DeleteLanguageCommand((LanguageId)filter.Id);
        return CommandDispatcher.Dispatch(command);
    }

    public override Task<LanguageDto> GetAsync(EntityByIdFilter filter)
    {
        var query = new GetLanguageByIdQuery((LanguageId)filter.Id);
        return QueryDispatcher.Dispatch(query);
    }

    public override async Task<PagedResult<LanguageDto>> GetListAsync(LanguageFilterDto filter)
    {
        var languageDtos = await QueryDispatcher.Dispatch(new GetLanguagesQuery());

        return new PagedResult<LanguageDto>
        {
            Items = languageDtos
        };
    }
}