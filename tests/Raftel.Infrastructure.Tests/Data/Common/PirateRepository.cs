using Raftel.Domain.Tests.Common.Domain;
using Raftel.Infrastructure.Data;

namespace Raftel.Infrastructure.Tests.Data.Common;

public class PirateRepository(TestingRaftelDbContext dbContext)
    : EfRepository<TestingRaftelDbContext, Pirate, PirateId>(dbContext), IPirateRepository
{
}