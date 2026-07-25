using MediatR;
using Security.API.Common;
using Security.API.Common.Auth;
using Security.API.Common.ErrorMapping;
using Security.API.Contracts.Auth;
using Security.API.Contracts.Users;
using Security.Application.Common.Results;
using Security.Application.Users.GetCurrentUser;
using Security.Application.Users.UpdateProfile;

namespace Security.API.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags(ApiTags.Users)
            .RequireAuthorization();

        group.MapGet("/me", GetCurrentUserAsync)
            .WithName("GetCurrentUser")
            .WithSummary("Gets the authenticated user.")
            .WithDescription("Returns the current authenticated user profile together with resolved permissions from claims.")
            .Produces<CurrentUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi();

        group.MapPut("/me/update", UpdateCurrentUserAsync)
            .WithName("UpdateCurrentUser")
            .WithSummary("Updates the authenticated user's profile.")
            .WithDescription("Updates the first name, last name and/or student type of the current user. Only provided fields are changed.")
            .Accepts<UpdateCurrentUserRequest>("application/json")
            .Produces<CurrentUserResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi();

        return app;
    }

    private static async Task<IResult> GetCurrentUserAsync(
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var currentUser = httpContext.User.ToCurrentUser();

        var result = await sender.Send(new GetCurrentUserQuery(currentUser.UserId), cancellationToken);
        if (result.IsFailure)
        {
            return httpContext.ToApiResult((Result)result);
        }

        return Results.Ok(ToCurrentUserResponse(result.Value, currentUser));
    }

    private static async Task<IResult> UpdateCurrentUserAsync(
        UpdateCurrentUserRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var currentUser = httpContext.User.ToCurrentUser();

        var command = new UpdateCurrentUserProfileCommand(
            currentUser.UserId,
            request.FirstName,
            request.LastName,
            request.StudentType);

        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return httpContext.ToApiResult((Result)result);
        }

        return Results.Ok(ToCurrentUserResponse(result.Value, currentUser));
    }

    private static CurrentUserResponse ToCurrentUserResponse(CurrentUserProfileDto profile, CurrentUser currentUser)
    {
        return new CurrentUserResponse(
            profile.Id,
            profile.Email,
            profile.FirstName,
            profile.LastName,
            profile.StudentType,
            profile.EmailVerified,
            currentUser.SessionId,
            currentUser.AccessTokenJti,
            currentUser.Permissions);
    }
}
