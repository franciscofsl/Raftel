using Microsoft.AspNetCore.Routing;
using Raftel.Api.Server.AutoEndpoints;
using Raftel.Application.Features.Users.AssignRoleToUser;
using Raftel.Application.Features.Users.CreateUser;
using Raftel.Application.Features.Users.DeleteUser;
using Raftel.Application.Features.Users.EditUser;
using Raftel.Application.Features.Users.GetUserProfile;
using Raftel.Application.Features.Users.RegisterUser;

namespace Raftel.Api.Server.Features.Users;

public static class UsersDependencyInjection
{
    public static IEndpointRouteBuilder AddRaftelUsers(this IEndpointRouteBuilder app)
    {
        app.AddEndpointGroup(group =>
        {
            group.Name = "Users";
            group.BaseUri = "/api/users";
            group.AddCommand<RegisterUserCommand>("register", HttpMethod.Post);
            group.AddQuery<GetUserProfileQuery, GetUserProfileResponse>("me", HttpMethod.Get);
            group.AddCommand<AssignRoleToUserCommand>("{userId}/roles", HttpMethod.Post);
            group.AddCommand<CreateUserCommand>("", HttpMethod.Post);
            group.AddCommand<EditUserCommand>("{userId}", HttpMethod.Put);
            group.AddCommand<DeleteUserCommand>("{userId}", HttpMethod.Delete);
        });

        return app;
    }
}

