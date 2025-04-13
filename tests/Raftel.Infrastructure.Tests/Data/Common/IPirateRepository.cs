using Raftel.Domain.Abstractions;
using Raftel.Domain.Tests.Common.Domain;

namespace Raftel.Infrastructure.Tests.Data.Common;

public interface IPirateRepository : IRepository<Pirate, PirateId>
{
}