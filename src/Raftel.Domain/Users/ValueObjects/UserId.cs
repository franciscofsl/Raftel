using Raftel.Domain.BaseTypes;

namespace Raftel.Domain.Users;

public sealed record UserId : TypedGuidId
{
    public UserId(Guid value) : base(value)
    {
    }

    public static UserId New() => new(NewGuid());
}