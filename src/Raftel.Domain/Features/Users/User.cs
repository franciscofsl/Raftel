using Raftel.Domain.Abstractions;
using Raftel.Domain.BaseTypes;
using Raftel.Domain.Features.Authorization;
using Raftel.Domain.Features.Users.ValueObjects;

namespace Raftel.Domain.Features.Users;

public sealed class User : AggregateRoot<UserId>
{
    private readonly List<Role> _roleIds = new();

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

    public Result AssignRole(Role role)
    {
        if (HasRole(role))
        {
            return Result.Failure(UserErrors.RoleAlreadyAssigned);
        }

        _roleIds.Add(role);
        return Result.Success();
    }

    private bool HasRole(Role role)
    {
        return _roleIds.Contains(role);
    }
}