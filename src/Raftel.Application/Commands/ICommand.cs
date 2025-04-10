using Raftel.Application.Abstractions;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Commands;

/// <summary>
/// Represents a command that does not return a value other than a <see cref="Result"/>.
/// </summary>
public interface ICommand : IRequest<Result>;