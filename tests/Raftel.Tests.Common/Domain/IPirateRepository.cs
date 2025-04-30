using Raftel.Domain.Abstractions;

namespace Raftel.Tests.Common.Domain;

public interface IPirateRepository : IRepository<Pirate, PirateId>
{
}