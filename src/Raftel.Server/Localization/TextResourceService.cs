using System.ServiceModel;
using Raftel.Application.Contracts.Localization;
using Raftel.Application.Cqrs.Commands;
using Raftel.Application.Cqrs.Queries;
using Raftel.Application.Localization.Commands.AddTranslationResource;
using Raftel.Application.Localization.Commands.Create;
using Raftel.Application.Localization.Commands.Delete;
using Raftel.Application.Localization.Commands.DeleteTranslationResource;
using Raftel.Application.Localization.Commands.Update;
using Raftel.Application.Localization.Commands.UpdateTranslationResource;
using Raftel.Application.Localization.Queries.ById;
using Raftel.Application.Localization.Queries.GetList;
using Raftel.Application.Localization.Queries.GetTextResources;
using Raftel.Blazor.Shared.Grpc.Filters;
using Raftel.Blazor.Shared.Localization;
using Raftel.Core.Localization.ValueObjects;
using Raftel.Server.Grpc;

namespace Raftel.Server.Localization;

[ServiceContract]
public class TextResourceService(IQueryDispatcher queryDispatcher, ICommandDispatcher commandDispatcher)
    : CrudService<TextResourceDto, CreateTextResourceDto, TextResourceDto, TextResourceFilterDto>(queryDispatcher,
            commandDispatcher),
        ITextResourceService
{
    public override async Task<TextResourceDto> CreateAsync(CreateTextResourceDto createDto)
    {
        var command =
            new AddTranslationResourceCommand((LanguageId)createDto.LanguageId, createDto.Value, createDto.Key);

        var result = await CommandDispatcher.Dispatch(command);

        return new TextResourceDto
        {
            Id = result.Value.Id,
            Value = result.Value.Value,
            Key = result.Value.Key,
        };
    }

    public override async Task<TextResourceDto> UpdateAsync(TextResourceDto dto)
    {
        var command = new UpdateTranslationResourceCommand((LanguageId)dto.LanguageId, dto.Value, dto.Key);
        var result = await CommandDispatcher.Dispatch(command);
        return new TextResourceDto
        {
            Id = result.Value.Id,
            Value = result.Value.Value,
            Key = result.Value.Key,
        };
    }

    public override Task DeleteAsync(EntityByIdFilter filter)
    {
        return Task.CompletedTask;
    }

    public override Task<TextResourceDto> GetAsync(EntityByIdFilter filter)
    {
        return Task.FromResult<TextResourceDto>(null);
    }

    public override async Task<PagedResult<TextResourceDto>> GetListAsync(TextResourceFilterDto filter)
    {
        var items = await QueryDispatcher.Dispatch(new GetTextResourcesQuery((LanguageId)filter.LanguageId));

        return new PagedResult<TextResourceDto>
        {
            Items = items
        };
    }
}