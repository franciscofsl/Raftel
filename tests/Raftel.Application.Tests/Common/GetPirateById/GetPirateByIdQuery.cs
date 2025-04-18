using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Tests.Common.GetPirateById;
public sealed record GetPirateByIdQuery() : IQuery<GetPirateByIdResponse>;