using Raftel.Demo.Domain.Common.ValueObjects;
using Raftel.Demo.Domain.Pirates.DevilFruits;
using Raftel.Demo.Domain.Pirates.ValueObjects;
using Raftel.Domain.Abstractions;
using Raftel.Domain.BaseTypes;

namespace Raftel.Demo.Domain.Pirates;

public class Pirate : AggregateRoot<PirateId>
{
    private BodyType _bodyType;
    private readonly List<DevilFruit> _eatenDevilFruits = new();

    private Pirate(Name name, Bounty bounty, BodyType bodyType) : this()
    {
        Name = name;
        Bounty = bounty;
        _bodyType = bodyType;
    }

    private Pirate() : base(PirateId.New())
    {
    }

    public Name Name { get; private set; }
    public Bounty Bounty { get; set; }
    public bool IsKing { get; private set; }

    public IReadOnlyCollection<DevilFruit> EatenDevilFruits => _eatenDevilFruits.AsReadOnly();

    public static Pirate Normal(Name name, Bounty bounty)
    {
        return new(name, bounty, BodyType.Normal);
    }
    
    public static Pirate Special(Name name, Bounty bounty)
    {
        return new(name, bounty, BodyType.Special);
    }

    public void FoundOnePiece()
    {
        IsKing = true;
    }

    public Result EatFruit(DevilFruit fruit)
    {
        if (_bodyType is BodyType.Normal && _eatenDevilFruits.Any())
        {
            return Result.Failure(PirateErrors.CannotEatMoreThanOneDevilFruit);
        }

        _eatenDevilFruits.Add(fruit);
        return Result.Success();
    }
}