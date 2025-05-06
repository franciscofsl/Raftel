using Raftel.Demo.Domain.Pirates.ValueObjects;
using Raftel.Domain.Abstractions;

namespace Raftel.Demo.Domain.Pirates;

public interface IPirateRepository : IRepository<Pirate, PirateId>
{
}