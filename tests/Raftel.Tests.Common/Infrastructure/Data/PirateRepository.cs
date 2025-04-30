using Raftel.Infrastructure.Data;
using Raftel.Tests.Common.Domain;

namespace Raftel.Tests.Common.Infrastructure.Data;

public class PirateRepository(TestingRaftelDbContext dbContext)
    : EfRepository<TestingRaftelDbContext, Pirate, PirateId>(dbContext), IPirateRepository
{
}