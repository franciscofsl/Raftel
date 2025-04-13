using Raftel.Domain.Abstractions;
using Raftel.Infrastructure.Tests.Common.PiratesEntities;

namespace Raftel.Infrastructure.Tests.Data.Common;

public interface IPirateRepository : IRepository<Pirate, PirateId>
{
}