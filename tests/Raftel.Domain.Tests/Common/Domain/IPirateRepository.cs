using Raftel.Domain.Abstractions;

namespace Raftel.Domain.Tests.Common.Domain;

public interface IPirateRepository : IRepository<Pirate, PirateId>
{
}