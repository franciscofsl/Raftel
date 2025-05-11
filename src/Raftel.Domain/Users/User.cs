using Raftel.Domain.BaseTypes;
using Raftel.Domain.Users.ValueObjects;

namespace Raftel.Domain.Users;

public sealed class User : AggregateRoot<UserId>
{
    private User()
    {
    }

    private User(Email email, string name, string surname) : base(UserId.New())
    {
        Email = email;
        Name = name;
        Surname = surname;
    }

    public Email Email { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }

    public static User Create(Email email, string name, string surname)
    {
        return new User(email, name, surname);
    }
}