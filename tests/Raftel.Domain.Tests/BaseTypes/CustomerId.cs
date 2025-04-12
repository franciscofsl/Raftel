using Raftel.Domain.BaseTypes;

namespace Raftel.Domain.Tests.BaseTypes;

public sealed record CustomerId : TypedGuidId
{
    public CustomerId(Guid value) : base(value) { }
    public static CustomerId Create() => new(Guid.NewGuid());
}