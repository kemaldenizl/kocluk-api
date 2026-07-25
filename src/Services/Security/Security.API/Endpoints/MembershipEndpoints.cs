using MediatR;
using Security.API.Common;
using Security.API.Common.Auth;
using Security.API.Common.ErrorMapping;
using Security.API.Contracts.Memberships;
using Security.Application.Memberships.AddMember;
using Security.Application.Memberships.AssignRoleToMember;
using Security.Application.Memberships.Dtos;
using Security.Application.Memberships.GetMyMemberships;
using Security.Application.Memberships.GetTenantMembers;
using Security.Application.Memberships.RemoveMember;
using Security.Application.Memberships.RemoveRoleFromMember;
using Security.Application.Memberships.SuspendMember;
using Security.Domain.Authorization;
using Security.Infrastructure.RateLimiting;

namespace Security.API.Endpoints;

/// <summary>
/// Tenant membership management. Kept on a Security-owned path (<c>/api/memberships</c>) so it does not
/// collide with the Tenant service's <c>/api/tenants</c> routes at the gateway. Authorization is decided
/// server-side per tenant (system admin, or members.manage/members.read via an active membership).
/// </summary>
public static class MembershipEndpoints
{
    public static IEndpointRouteBuilder MapMembershipEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/memberships")
            .WithTags(ApiTags.Memberships)
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicyNames.Admin);

        group.MapGet("/me", GetMyMembershipsAsync)
            .WithName("GetMyMemberships")
            .WithSummary("Lists the current user's memberships (history included).")
            .Produces<IReadOnlyList<MembershipResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithOpenApi();

        group.MapGet("/", GetTenantMembersAsync)
            .WithName("GetTenantMembers")
            .WithSummary("Lists the members of a tenant.")
            .Produces<IReadOnlyList<TenantMemberResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithOpenApi();

        group.MapPost("/", AddMemberAsync)
            .WithName("AddMember")
            .WithSummary("Adds an existing user to a tenant.")
            .Accepts<AddMemberRequest>("application/json")
            .Produces<MembershipResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithOpenApi();

        group.MapDelete("/{userId:guid}", RemoveMemberAsync)
            .WithName("RemoveMember")
            .WithSummary("Removes a member from a tenant (soft, keeps history).")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi();

        group.MapPost("/{userId:guid}/suspend", SuspendMemberAsync)
            .WithName("SuspendMember")
            .WithSummary("Suspends a member in a tenant.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi();

        group.MapPost("/{userId:guid}/reactivate", ReactivateMemberAsync)
            .WithName("ReactivateMember")
            .WithSummary("Reactivates a suspended or removed member in a tenant.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi();

        group.MapPost("/{userId:guid}/roles", AssignRoleToMemberAsync)
            .WithName("AssignRoleToMember")
            .WithSummary("Grants a tenant role to a member.")
            .Accepts<AssignMemberRoleRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi();

        group.MapDelete("/{userId:guid}/roles/{roleId:guid}", RemoveRoleFromMemberAsync)
            .WithName("RemoveRoleFromMember")
            .WithSummary("Removes a tenant role from a member.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi();

        return app;
    }

    private static async Task<IResult> GetMyMembershipsAsync(
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var currentUser = httpContext.User.ToCurrentUser();

        var result = await sender.Send(new GetMyMembershipsQuery(currentUser.UserId), cancellationToken);
        if (result.IsFailure)
            return httpContext.ToApiResult(result);

        return Results.Ok(result.Value.Select(ToResponse).ToArray());
    }

    private static async Task<IResult> GetTenantMembersAsync(
        Guid tenantId,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var currentUser = httpContext.User.ToCurrentUser();

        var result = await sender.Send(new GetTenantMembersQuery(tenantId, currentUser.UserId), cancellationToken);
        if (result.IsFailure)
            return httpContext.ToApiResult(result);

        return Results.Ok(result.Value.Select(ToResponse).ToArray());
    }

    private static async Task<IResult> AddMemberAsync(
        AddMemberRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var currentUser = httpContext.User.ToCurrentUser();

        var command = new AddMemberCommand(
            request.TenantId,
            currentUser.UserId,
            request.UserId,
            request.Email,
            request.RoleIds ?? []);

        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
            return httpContext.ToApiResult(result);

        var response = ToResponse(result.Value);
        return Results.Created($"/api/memberships/{response.UserId}", response);
    }

    private static async Task<IResult> RemoveMemberAsync(
        Guid userId,
        Guid tenantId,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var currentUser = httpContext.User.ToCurrentUser();

        var result = await sender.Send(
            new RemoveMemberCommand(tenantId, currentUser.UserId, userId), cancellationToken);

        return httpContext.ToApiResult(result);
    }

    private static async Task<IResult> SuspendMemberAsync(
        Guid userId,
        Guid tenantId,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var currentUser = httpContext.User.ToCurrentUser();

        var result = await sender.Send(
            new SuspendMemberCommand(tenantId, currentUser.UserId, userId, Suspend: true), cancellationToken);

        return httpContext.ToApiResult(result);
    }

    private static async Task<IResult> ReactivateMemberAsync(
        Guid userId,
        Guid tenantId,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var currentUser = httpContext.User.ToCurrentUser();

        var result = await sender.Send(
            new SuspendMemberCommand(tenantId, currentUser.UserId, userId, Suspend: false), cancellationToken);

        return httpContext.ToApiResult(result);
    }

    private static async Task<IResult> AssignRoleToMemberAsync(
        Guid userId,
        Guid tenantId,
        AssignMemberRoleRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var currentUser = httpContext.User.ToCurrentUser();

        var result = await sender.Send(
            new AssignRoleToMemberCommand(tenantId, currentUser.UserId, userId, request.RoleId), cancellationToken);

        return httpContext.ToApiResult(result);
    }

    private static async Task<IResult> RemoveRoleFromMemberAsync(
        Guid userId,
        Guid roleId,
        Guid tenantId,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var currentUser = httpContext.User.ToCurrentUser();

        var result = await sender.Send(
            new RemoveRoleFromMemberCommand(tenantId, currentUser.UserId, userId, roleId), cancellationToken);

        return httpContext.ToApiResult(result);
    }

    private static MembershipResponse ToResponse(MembershipDto dto) => new(
        dto.Id, dto.UserId, dto.TenantId, dto.Status, dto.Roles, dto.JoinedAtUtc, dto.UpdatedAtUtc);

    private static TenantMemberResponse ToResponse(TenantMemberDto dto) => new(
        dto.UserId, dto.Email, dto.FirstName, dto.LastName, dto.Status, dto.Roles, dto.JoinedAtUtc, dto.UpdatedAtUtc);
}
