using Raftel.Application.Cqrs.Commands;
using Raftel.Shared.Results;

namespace Raftel.Application.BlobStorage.Commands.AddDocument;

public record AddDocumentCommand(Guid? FolderId, string Name, byte[] Content) : ICommand<Result>;