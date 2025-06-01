using Microsoft.AspNetCore.Routing;
using Raftel.Api.Server.AutoEndpoints;
using Raftel.Application.Features.Users.AssignRoleToUser;
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
            group.AddCommand<RegisterUserCommand>("register", HttpMethod.Post).AllowAnonymous();
            group.AddQuery<GetUserProfileQuery, GetUserProfileResponse>("me", HttpMethod.Get);
            group.AddCommand<AssignRoleToUserCommand>("{userId}/roles", HttpMethod.Post);
        });

        return app;
    }
}

