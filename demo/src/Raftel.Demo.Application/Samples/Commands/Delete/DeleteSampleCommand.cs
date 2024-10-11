using Raftel.Application.Cqrs.Commands;
using Raftel.Demo.Core.Samples.ValueObjects;

namespace Raftel.Demo.Application.Samples.Commands.Delete;

public record DeleteSampleCommand(SampleId Id) : ICommand<bool>;