using Raftel.Core.BaseTypes;

namespace Raftel.Inkventory.Core.Customers;

public sealed record CustomerId : TypedGuidId
{
    public CustomerId(Guid value) : base(value)
    {
    }

    public static CustomerId New() => new(NewGuid());
}