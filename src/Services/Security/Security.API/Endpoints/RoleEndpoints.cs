using MediatR;
using Security.API.Common;
using Security.API.Common.ErrorMapping;
using Security.API.Contracts.Permissions;
using Security.API.Contracts.Roles;
using Security.Application.Roles.AddPermissionToRole;
using Security.Application.Roles.CreateRole;
using Security.Application.Roles.DeleteRole;
using Security.Application.Roles.GetRoleById;
using Security.Application.Roles.GetRoles;
using Security.Application.Roles.RemovePermissionFromRole;
using Security.Application.Roles.RenameRole;
using Security.Domain.Authorization;
using Security.Infrastructure.RateLimiting;

namespace Security.API.Endpoints;

public static class RoleEndpoints
{
    public static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/roles")
            .WithTags(ApiTags.Admin)
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicyNames.Admin);

        group.MapGet("/", GetRolesAsync)
            .RequireAuthorization(PermissionCodes.RolesRead)
            .WithName("GetRoles")
            .WithSummary("Gets all roles.")
            .WithDescription("Returns all roles with their assigned permissions.")
            .Produces<IReadOnlyCollection<RoleResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithOpenApi();

        group.MapGet("/{id:guid}", GetRoleByIdAsync)
            .RequireAuthorization(PermissionCodes.RolesRead)
            .WithName("GetRoleById")
            .WithSummary("Gets a role by id.")
            .WithDescription("Returns a single role with its assigned permissions.")
            .Produces<RoleResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithOpenApi();

        group.MapPost("/", CreateRoleAsync)
            .RequireAuthorization(PermissionCodes.RolesManage)
            .WithName("CreateRole")
            .WithSummary("Creates a new role.")
            .WithDescription("Creates a new role with a unique name.")
            .Accepts<CreateRoleRequest>("application/json")
            .Produces<RoleResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithOpenApi();

        group.MapPut("/{id:guid}", RenameRoleAsync)
            .RequireAuthorization(PermissionCodes.RolesManage)
            .WithName("RenameRole")
            .WithSummary("Renames a role.")
            .WithDescription("Updates the name of an existing role.")
            .Accepts<RenameRoleRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithOpenApi();

        group.MapDelete("/{id:guid}", DeleteRoleAsync)
            .RequireAuthorization(PermissionCodes.RolesManage)
            .WithName("DeleteRole")
            .WithSummary("Deletes a role.")
            .WithDescription("Deletes a role and revokes the access tokens of affected users.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithOpenApi();

        group.MapPost("/{id:guid}/permissions", AddPermissionToRoleAsync)
            .RequireAuthorization(PermissionCodes.RolesManage)
            .WithName("AddPermissionToRole")
            .WithSummary("Adds a permission to a role.")
            .WithDescription("Assigns a permission to a role and revokes the access tokens of affected users.")
            .Accepts<AddPermissionToRoleRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithOpenApi();

        group.MapDelete("/{id:guid}/permissions/{permissionId:guid}", RemovePermissionFromRoleAsync)
            .RequireAuthorization(PermissionCodes.RolesManage)
            .WithName("RemovePermissionFromRole")
            .WithSummary("Removes a permission from a role.")
            .WithDescription("Removes a permission from a role and revokes the access tokens of affected users.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithOpenApi();

        return app;
    }

    private static async Task<IResult> GetRolesAsync(
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetRolesQuery(), cancellationToken);

        if (result.IsFailure)
            return httpContext.ToApiResult(result);

        var response = result.Value
            .Select(ToRoleResponse)
            .ToArray();

        return Results.Ok(response);
    }

    private static async Task<IResult> GetRoleByIdAsync(
        Guid id,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetRoleByIdQuery(id), cancellationToken);

        if (result.IsFailure)
            return httpContext.ToApiResult(result);

        return Results.Ok(ToRoleResponse(result.Value));
    }

    private static async Task<IResult> CreateRoleAsync(
        CreateRoleRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateRoleCommand(request.Name), cancellationToken);

        if (result.IsFailure)
            return httpContext.ToApiResult(result);

        var response = ToRoleResponse(result.Value);

        return Results.Created($"/api/roles/{response.Id}", response);
    }

    private static async Task<IResult> RenameRoleAsync(
        Guid id,
        RenameRoleRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RenameRoleCommand(id, request.Name), cancellationToken);

        return httpContext.ToApiResult(result);
    }

    private static async Task<IResult> DeleteRoleAsync(
        Guid id,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteRoleCommand(id), cancellationToken);

        return httpContext.ToApiResult(result);
    }

    private static async Task<IResult> AddPermissionToRoleAsync(
        Guid id,
        AddPermissionToRoleRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AddPermissionToRoleCommand(id, request.PermissionId), cancellationToken);

        return httpContext.ToApiResult(result);
    }

    private static async Task<IResult> RemovePermissionFromRoleAsync(
        Guid id,
        Guid permissionId,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemovePermissionFromRoleCommand(id, permissionId), cancellationToken);

        return httpContext.ToApiResult(result);
    }

    private static RoleResponse ToRoleResponse(Application.Roles.Dtos.RoleDto role)
    {
        var permissions = role.Permissions
            .Select(x => new PermissionResponse(x.Id, x.Code))
            .ToArray();

        return new RoleResponse(role.Id, role.Name, permissions);
    }
}
