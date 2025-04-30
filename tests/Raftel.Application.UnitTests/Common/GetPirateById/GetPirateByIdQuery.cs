using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.UnitTests.Common.GetPirateById;
public sealed record GetPirateByIdQuery() : IQuery<GetPirateByIdResponse>;