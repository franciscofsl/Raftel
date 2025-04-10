using Raftel.Application.Abstractions;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Commands;

public interface ICommand : IRequest<Result> { }