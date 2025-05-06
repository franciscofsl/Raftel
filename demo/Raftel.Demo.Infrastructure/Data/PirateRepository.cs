using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Pirates.ValueObjects;
using Raftel.Infrastructure.Data;

namespace Raftel.Demo.Infrastructure.Data;

public class PirateRepository(TestingRaftelDbContext dbContext)
    : EfRepository<TestingRaftelDbContext, Pirate, PirateId>(dbContext), IPirateRepository
{
}