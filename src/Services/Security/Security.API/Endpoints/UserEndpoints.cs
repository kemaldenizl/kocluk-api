using MediatR;
using Security.API.Common;
using Security.API.Common.Auth;
using Security.API.Common.ErrorMapping;
using Security.API.Contracts.Auth;
using Security.API.Contracts.Users;
using Security.Application.Users.AssignRole;
using Security.Application.Users.RemoveRole;
using Security.Domain.Authorization;
using Security.Infrastructure.RateLimiting;

namespace Security.API.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags(ApiTags.Users)
            .RequireAuthorization();

        group.MapGet("/me", (HttpContext httpContext) =>
        {
            var currentUser = httpContext.User.ToCurrentUser();

            var response = new CurrentUserResponse(
                currentUser.UserId,
                currentUser.Email,
                currentUser.SessionId,
                currentUser.AccessTokenJti,
                currentUser.Permissions
            );

            return Results.Ok(response);
        })
        .WithName("GetCurrentUser")
        .WithSummary("Gets the authenticated user.")
        .WithDescription("Returns the current authenticated user and resolved permissions from claims.")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithOpenApi();

        group.MapPost("/{userId:guid}/roles", AssignRoleToUserAsync)
            .RequireAuthorization(PermissionCodes.UsersManage)
            .RequireRateLimiting(RateLimitPolicyNames.Admin)
            .WithTags(ApiTags.Admin)
            .WithName("AssignRoleToUser")
            .WithSummary("Assigns a role to a user.")
            .WithDescription("Assigns a role to a user and revokes the user's access tokens.")
            .Accepts<AssignRoleRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithOpenApi();

        group.MapDelete("/{userId:guid}/roles/{roleId:guid}", RemoveRoleFromUserAsync)
            .RequireAuthorization(PermissionCodes.UsersManage)
            .RequireRateLimiting(RateLimitPolicyNames.Admin)
            .WithTags(ApiTags.Admin)
            .WithName("RemoveRoleFromUser")
            .WithSummary("Removes a role from a user.")
            .WithDescription("Removes a role from a user and revokes the user's access tokens.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithOpenApi();

        return app;
    }

    private static async Task<IResult> AssignRoleToUserAsync(
        Guid userId,
        AssignRoleRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AssignRoleToUserCommand(userId, request.RoleId), cancellationToken);

        return httpContext.ToApiResult(result);
    }

    private static async Task<IResult> RemoveRoleFromUserAsync(
        Guid userId,
        Guid roleId,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveRoleFromUserCommand(userId, roleId), cancellationToken);

        return httpContext.ToApiResult(result);
    }
}
