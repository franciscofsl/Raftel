namespace Raftel.Core.BaseTypes;

public abstract record TypedGuidId : TypedId<Guid>
{
    protected TypedGuidId(Guid value) : base(value)
    {
    }

    public static Guid NewGuid() => Guid.CreateVersion7();
}