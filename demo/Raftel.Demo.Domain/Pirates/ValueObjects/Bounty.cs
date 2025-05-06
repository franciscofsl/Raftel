namespace Raftel.Demo.Domain.Pirates.ValueObjects;

public readonly record struct Bounty
{
    private readonly uint _value;

    public Bounty(uint value)
    {
        if (value < 0)
            throw new ArgumentException("Bounty cannot be negative.", nameof(value));

        _value = value;
    }

    public override string ToString() => $"{_value:N0} berries";

    public static implicit operator uint(Bounty bounty) => bounty._value;
    public static implicit operator Bounty(uint value) => new(value);
}