using MediatR;
using Security.API.Common;
using Security.API.Common.ErrorMapping;
using Security.API.Contracts.Permissions;
using Security.Application.Permissions.CreatePermission;
using Security.Application.Permissions.DeletePermission;
using Security.Application.Permissions.GetPermissions;
using Security.Domain.Authorization;
using Security.Infrastructure.RateLimiting;

namespace Security.API.Endpoints;

public static class PermissionEndpoints
{
    public static IEndpointRouteBuilder MapPermissionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/permissions")
            .WithTags(ApiTags.Admin)
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicyNames.Admin);

        group.MapGet("/", GetPermissionsAsync)
            .RequireAuthorization(PermissionCodes.PermissionsRead)
            .WithName("GetPermissions")
            .WithSummary("Gets all permissions.")
            .WithDescription("Returns the full permission catalog.")
            .Produces<IReadOnlyCollection<PermissionResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithOpenApi();

        group.MapPost("/", CreatePermissionAsync)
            .RequireAuthorization(PermissionCodes.PermissionsManage)
            .WithName("CreatePermission")
            .WithSummary("Creates a new permission.")
            .WithDescription("Creates a new permission with a unique code.")
            .Accepts<CreatePermissionRequest>("application/json")
            .Produces<PermissionResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithOpenApi();

        group.MapDelete("/{id:guid}", DeletePermissionAsync)
            .RequireAuthorization(PermissionCodes.PermissionsManage)
            .WithName("DeletePermission")
            .WithSummary("Deletes a permission.")
            .WithDescription("Deletes a permission that is not assigned to any role.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithOpenApi();

        return app;
    }

    private static async Task<IResult> GetPermissionsAsync(
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPermissionsQuery(), cancellationToken);

        if (result.IsFailure)
            return httpContext.ToApiResult(result);

        var response = result.Value
            .Select(x => new PermissionResponse(x.Id, x.Code))
            .ToArray();

        return Results.Ok(response);
    }

    private static async Task<IResult> CreatePermissionAsync(
        CreatePermissionRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreatePermissionCommand(request.Code), cancellationToken);

        if (result.IsFailure)
            return httpContext.ToApiResult(result);

        var response = new PermissionResponse(result.Value.Id, result.Value.Code);

        return Results.Created($"/api/permissions/{response.Id}", response);
    }

    private static async Task<IResult> DeletePermissionAsync(
        Guid id,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeletePermissionCommand(id), cancellationToken);

        return httpContext.ToApiResult(result);
    }
}
