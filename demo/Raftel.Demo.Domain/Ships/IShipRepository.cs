using Raftel.Domain.Abstractions;

namespace Raftel.Demo.Domain.Ships;

public interface IShipRepository : IRepository<Ship, ShipId>
{
}