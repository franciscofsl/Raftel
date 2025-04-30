using Raftel.Domain.Abstractions;

namespace Raftel.Demo.Domain.Pirates;

public interface IPirateRepository : IRepository<Pirate, PirateId>
{
}