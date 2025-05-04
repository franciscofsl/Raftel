using Raftel.Demo.Domain.Ships;
using Raftel.Domain.Abstractions;

namespace Raftel.Demo.Domain.Pirates;

public interface IShipRepository : IRepository<Ship, ShipId>
{
}