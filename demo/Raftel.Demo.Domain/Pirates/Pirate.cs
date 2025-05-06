using Raftel.Demo.Domain.Common.ValueObjects;
using Raftel.Demo.Domain.Pirates.DevilFruits;
using Raftel.Demo.Domain.Pirates.ValueObjects;
using Raftel.Domain.Abstractions;
using Raftel.Domain.BaseTypes;

namespace Raftel.Demo.Domain.Pirates;

public class Pirate : AggregateRoot<PirateId>
{
    private readonly DevilFruitCollection _eatenDevilFruits;
    private readonly BodyType _bodyType;

    private Pirate(Name name, Bounty bounty, BodyType bodyType) : this()
    {
        Name = name;
        Bounty = bounty;
        _bodyType = bodyType;
        _eatenDevilFruits = new DevilFruitCollection();
    }

    private Pirate() : base(PirateId.New())
    {
    }

    public Name Name { get; }
    public Bounty Bounty { get; set; }
    public bool IsKing { get; private set; }

    public IReadOnlyCollection<DevilFruit> EatenDevilFruits => _eatenDevilFruits.AsReadOnly();

    public static Pirate Normal(Name name, Bounty bounty) => new(name, bounty, BodyType.Normal);

    public static Pirate Special(Name name, Bounty bounty) => new(name, bounty, BodyType.Special);

    public void FoundOnePiece() => IsKing = true;

    public Result EatFruit(DevilFruit fruit)
    {
        if (!CanEatFruit())
        {
            return Result.Failure(PirateErrors.CannotEatMoreThanOneDevilFruit);
        }

        _eatenDevilFruits.Add(fruit);
        return Result.Success();
    }

    private bool CanEatFruit() => _bodyType != BodyType.Normal || !_eatenDevilFruits.HasAny();
}