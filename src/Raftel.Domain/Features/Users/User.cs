﻿using Raftel.Domain.BaseTypes;
using Raftel.Domain.Features.Users.ValueObjects;

namespace Raftel.Domain.Features.Users;

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
    public string IdentityUserId { get; private set; }

    public static User Create(Email email, string name, string surname)
    {
        return new User(email, name, surname);
    }

    public void BindTo(string identityUserId)
    {
        if (string.IsNullOrEmpty(IdentityUserId))
        {
            IdentityUserId = identityUserId;
        }
    }
}