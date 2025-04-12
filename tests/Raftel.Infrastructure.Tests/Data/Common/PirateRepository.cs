using Raftel.Infrastructure.Data;
using Raftel.Infrastructure.Tests.Common.PiratesEntities;

namespace Raftel.Infrastructure.Tests.Data.Common;

public class PirateRepository(TestingRaftelDbContext dbContext)
    : EfRepository<TestingRaftelDbContext, Pirate, PirateId>(dbContext), IPirateRepository
{
}