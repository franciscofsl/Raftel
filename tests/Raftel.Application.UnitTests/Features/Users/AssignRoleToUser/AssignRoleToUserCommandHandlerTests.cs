using Raftel.Application.Features.Users.AssignRoleToUser;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Authorization;
using Raftel.Domain.Features.Authorization.ValueObjects;
using Raftel.Domain.Features.Users;
using Raftel.Domain.Features.Users.ValueObjects;
using Shouldly;

namespace Raftel.Application.UnitTests.Features.Users.AssignRoleToUser;

public sealed class AssignRoleToUserCommandHandlerTests
{
    private readonly IRepository<User, UserId> _userRepository = Substitute.For<IRepository<User, UserId>>();
    private readonly IRepository<Role, RoleId> _roleRepository = Substitute.For<IRepository<Role, RoleId>>();
    private readonly AssignRoleToUserCommandHandler _handler;

    public AssignRoleToUserCommandHandlerTests()
    {
        _handler = new AssignRoleToUserCommandHandler(_userRepository, _roleRepository);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnSuccess_When_RoleAssignedSuccessfully()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var command = new AssignRoleToUserCommand(userId, roleId);
        
        var user = User.Create("user@example.com", "Test", "User");
        var roleResult = Role.Create("TestRole", "Test Role Description");
        var role = roleResult.Value;
        
        _userRepository.GetByIdAsync(Arg.Is<UserId>(id => id == new UserId(userId)), Arg.Any<CancellationToken>())
            .Returns(user);
            
        _roleRepository.GetByIdAsync(Arg.Is<RoleId>(id => id == new RoleId(roleId)), Arg.Any<CancellationToken>())
            .Returns(role);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _userRepository.Received(1).Update(user);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnFailure_When_RoleAlreadyAssigned()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var command = new AssignRoleToUserCommand(userId, roleId);
        
        var user = User.Create("user@example.com", "Test", "User");
        var roleResult = Role.Create("TestRole", "Test Role Description");
        var role = roleResult.Value;
        
        user.AssignRole(role);
        
        _userRepository.GetByIdAsync(Arg.Is<UserId>(id => id == new UserId(userId)), Arg.Any<CancellationToken>())
            .Returns(user);
            
        _roleRepository.GetByIdAsync(Arg.Is<RoleId>(id => id == new RoleId(roleId)), Arg.Any<CancellationToken>())
            .Returns(role);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(UserErrors.RoleAlreadyAssigned);
        _userRepository.DidNotReceive().Update(user);
    }
}
