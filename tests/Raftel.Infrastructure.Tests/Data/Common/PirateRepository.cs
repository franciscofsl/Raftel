using Raftel.Infrastructure.Data;
using Raftel.Tests.Common.Domain;

namespace Raftel.Infrastructure.Tests.Data.Common;

public class PirateRepository(TestingRaftelDbContext dbContext)
    : EfRepository<TestingRaftelDbContext, Pirate, PirateId>(dbContext), IPirateRepository
{
}