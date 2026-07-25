using MediatR;
using Tenant.API.Authorization;
using Tenant.API.Common.ErrorMapping;
using Tenant.API.Contracts.Tenants;
using Tenant.Application.Tenants.ActivateTenant;
using Tenant.Application.Tenants.CreateTenant;
using Tenant.Application.Tenants.DeactivateTenant;
using Tenant.Application.Tenants.DeleteTenant;
using Tenant.Application.Tenants.Dtos;
using Tenant.Application.Tenants.GetTenantById;
using Tenant.Application.Tenants.GetTenants;
using Tenant.Application.Tenants.RenameTenant;

namespace Tenant.API.Endpoints;

public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tenants")
            .WithTags("Tenants")
            .RequireAuthorization();

        group.MapGet("/", GetTenantsAsync)
            .WithName("GetTenants")
            .WithSummary("Lists all tenants.")
            .Produces<IReadOnlyCollection<TenantResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithOpenApi();

        group.MapGet("/{id:guid}", GetTenantByIdAsync)
            .WithName("GetTenantById")
            .WithSummary("Gets a tenant by id.")
            .Produces<TenantResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi();

        group.MapPost("/", CreateTenantAsync)
            .RequireAuthorization(AuthorizationPolicies.SystemAdmin)
            .WithName("CreateTenant")
            .WithSummary("Creates a new tenant.")
            .Accepts<CreateTenantRequest>("application/json")
            .Produces<TenantResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithOpenApi();

        group.MapPut("/{id:guid}", RenameTenantAsync)
            .RequireAuthorization(AuthorizationPolicies.SystemAdmin)
            .WithName("RenameTenant")
            .WithSummary("Renames a tenant.")
            .Accepts<RenameTenantRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi();

        group.MapPost("/{id:guid}/activate", ActivateTenantAsync)
            .RequireAuthorization(AuthorizationPolicies.SystemAdmin)
            .WithName("ActivateTenant")
            .WithSummary("Activates a tenant.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi();

        group.MapPost("/{id:guid}/deactivate", DeactivateTenantAsync)
            .RequireAuthorization(AuthorizationPolicies.SystemAdmin)
            .WithName("DeactivateTenant")
            .WithSummary("Deactivates a tenant.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi();

        group.MapDelete("/{id:guid}", DeleteTenantAsync)
            .RequireAuthorization(AuthorizationPolicies.SystemAdmin)
            .WithName("DeleteTenant")
            .WithSummary("Deletes a tenant.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi();

        return app;
    }

    private static async Task<IResult> GetTenantsAsync(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTenantsQuery(), cancellationToken);

        if (result.IsFailure)
            return ApplicationResultMapper.MapFailure(result.Error);

        return Results.Ok(result.Value.Select(ToResponse).ToArray());
    }

    private static async Task<IResult> GetTenantByIdAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTenantByIdQuery(id), cancellationToken);

        return result.IsFailure
            ? ApplicationResultMapper.MapFailure(result.Error)
            : Results.Ok(ToResponse(result.Value));
    }

    private static async Task<IResult> CreateTenantAsync(
        CreateTenantRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateTenantCommand(request.Name, request.Slug), cancellationToken);

        if (result.IsFailure)
            return ApplicationResultMapper.MapFailure(result.Error);

        var response = ToResponse(result.Value);
        return Results.Created($"/api/tenants/{response.Id}", response);
    }

    private static async Task<IResult> RenameTenantAsync(
        Guid id,
        RenameTenantRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RenameTenantCommand(id, request.Name), cancellationToken);
        return result.ToApiResult();
    }

    private static async Task<IResult> ActivateTenantAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ActivateTenantCommand(id), cancellationToken);
        return result.ToApiResult();
    }

    private static async Task<IResult> DeactivateTenantAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeactivateTenantCommand(id), cancellationToken);
        return result.ToApiResult();
    }

    private static async Task<IResult> DeleteTenantAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteTenantCommand(id), cancellationToken);
        return result.ToApiResult();
    }

    private static TenantResponse ToResponse(TenantDto tenant) => new(
        tenant.Id,
        tenant.Name,
        tenant.Slug,
        tenant.IsActive,
        tenant.CreatedAtUtc,
        tenant.UpdatedAtUtc);
}
