using Raftel.Application.Contracts.Storage;
using Raftel.Application.Cqrs.Commands;
using Raftel.Shared.Results;

namespace Raftel.Application.BlobStorage.Commands.CreateFolder;

public record CreateFolderCommand(string Name) : ICommand<Result<FolderDto>>;