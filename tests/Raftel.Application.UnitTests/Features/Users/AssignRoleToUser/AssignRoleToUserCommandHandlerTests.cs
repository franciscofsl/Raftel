using Raftel.Application.Abstractions.Authentication;
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
    private readonly IUsersRepository _userRepository = Substitute.For<IUsersRepository>();
    private readonly IRolesRepository _roleRepository = Substitute.For<IRolesRepository>();
    private readonly IAuthenticationService _authenticationService = Substitute.For<IAuthenticationService>();
    private readonly AssignRoleToUserCommandHandler _handler;

    public AssignRoleToUserCommandHandlerTests()
    {
        _handler = new AssignRoleToUserCommandHandler(_userRepository, _roleRepository, _authenticationService);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnSuccess_When_RoleAssignedSuccessfully()
    {
        // Arrange
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

        _authenticationService.AssignRoleAsync(user, role, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _authenticationService.Received(1).AssignRoleAsync(user, role, Arg.Any<CancellationToken>());
        _userRepository.Received(1).Update(user);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnFailure_When_IdentityAssignRoleFails()
    {
        // Arrange
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
        
        var identityError = new Error("User.RoleAssignmentFailed", "Failed to assign role in Identity");
        _authenticationService.AssignRoleAsync(user, role, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(identityError));

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(identityError);
        _userRepository.DidNotReceive().Update(user);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnFailure_When_RoleAlreadyAssigned()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var command = new AssignRoleToUserCommand(userId, roleId);
        
        var user = User.Create("user@example.com", "Test", "User");
        var roleResult = Role.Create("TestRole", "Test Role Description");
        var role = roleResult.Value;
        
        user.AssignRole(role); // El rol ya está asignado al usuario
        
        _userRepository.GetByIdAsync(Arg.Is<UserId>(id => id == new UserId(userId)), Arg.Any<CancellationToken>())
            .Returns(user);
            
        _roleRepository.GetByIdAsync(Arg.Is<RoleId>(id => id == new RoleId(roleId)), Arg.Any<CancellationToken>())
            .Returns(role);

        _authenticationService.AssignRoleAsync(user, role, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(UserErrors.RoleAlreadyAssigned);
        _userRepository.DidNotReceive().Update(user);
    }
}
