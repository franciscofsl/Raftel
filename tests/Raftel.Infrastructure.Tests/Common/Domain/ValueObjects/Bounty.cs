namespace Raftel.Infrastructure.Tests.Common.PiratesEntities.ValueObjects;

public readonly record struct Bounty
{
    private readonly int _value;

    public Bounty(int value)
    {
        if (value < 0)
            throw new ArgumentException("Bounty cannot be negative.", nameof(value));

        _value = value;
    }

    public override string ToString() => $"{_value:N0} berries";

    public static implicit operator int(Bounty bounty) => bounty._value;
    public static implicit operator Bounty(int value) => new(value);
}