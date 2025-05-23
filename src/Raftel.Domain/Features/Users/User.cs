using Raftel.Domain.BaseTypes;
using Raftel.Domain.Features.Users.ValueObjects;

namespace Raftel.Domain.Features.Users;

public sealed class User : AggregateRoot<UserId>
{
    private User()
    {
    }

    private User(Email email, string name, string surname, string identityUserId) : base(UserId.New())
    {
        Email = email;
        Name = name;
        Surname = surname;
        IdentityUserId = identityUserId;
    }

    public Email Email { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
    public string IdentityUserId { get; set; }

    public static User Create(Email email, string name, string surname, string identityUserId)
    {
        return new User(email, name, surname, identityUserId);
    }
}