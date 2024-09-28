namespace Raftel.Core.BaseTypes;

public abstract record EntityId
{
    public Guid Value { get; protected internal set; }

    protected IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}