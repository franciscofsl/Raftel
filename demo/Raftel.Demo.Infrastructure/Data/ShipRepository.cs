using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Ships;
using Raftel.Infrastructure.Data;

namespace Raftel.Demo.Infrastructure.Data;

public class ShipRepository(TestingRaftelDbContext dbContext)
    : EfRepository<TestingRaftelDbContext, Ship, ShipId>(dbContext), IShipRepository
{
}