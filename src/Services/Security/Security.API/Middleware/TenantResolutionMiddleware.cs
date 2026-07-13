using Microsoft.Extensions.Options;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.Tenancy;
using Security.Application.Common.Security;
using Security.Infrastructure.Tenancy;

namespace Security.API.Middleware;

/// <summary>
/// Resolves the tenant for the current request when the service runs in multi-tenant mode:
/// authenticated requests take the tenant from the signed <c>tenant_id</c> JWT claim, anonymous
/// requests (login, register, ...) take it from the configured tenant header and validate it against
/// the tenant registry. Infrastructure paths (health, docs) are exempt. In single-tenant mode the
/// tenant is already pinned to the default tenant, so the middleware is a no-op.
/// </summary>
public sealed class TenantResolutionMiddleware(RequestDelegate next, IOptions<MultiTenancyOptions> options)
{
    private static readonly string[] ExemptPathPrefixes =
    [
        "/health",
        "/swagger",
        "/openapi"
    ];

    private readonly MultiTenancyOptions _options = options.Value;

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext,
        ITenantRepository tenantRepository)
    {
        if (!_options.Enabled || IsExempt(context.Request.Path))
        {
            await next(context);
            return;
        }

        // Authenticated requests: trust the signed, already-validated tenant claim.
        var claimValue = context.User.FindFirst(CustomClaimTypes.TenantId)?.Value;
        if (Guid.TryParse(claimValue, out var claimTenantId))
        {
            tenantContext.SetTenant(claimTenantId);
            await next(context);
            return;
        }

        // Anonymous requests: resolve from the tenant header and validate against the registry.
        var headerValue = context.Request.Headers[_options.HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            await WriteTenantProblemAsync(
                context,
                $"A tenant must be supplied via the '{_options.HeaderName}' header.");
            return;
        }

        var resolvedTenantId = await ResolveFromHeaderAsync(headerValue.Trim(), tenantRepository, context.RequestAborted);
        if (resolvedTenantId is null)
        {
            await WriteTenantProblemAsync(context, "The specified tenant is unknown or inactive.");
            return;
        }

        tenantContext.SetTenant(resolvedTenantId.Value);
        await next(context);
    }

    private static async Task<Guid?> ResolveFromHeaderAsync(
        string headerValue,
        ITenantRepository tenantRepository,
        CancellationToken cancellationToken)
    {
        if (Guid.TryParse(headerValue, out var tenantId))
        {
            return await tenantRepository.ExistsActiveAsync(tenantId, cancellationToken)
                ? tenantId
                : null;
        }

        var tenant = await tenantRepository.GetBySlugAsync(headerValue, cancellationToken);
        return tenant is { IsActive: true } ? tenant.Id : null;
    }

    private static bool IsExempt(PathString path)
    {
        if (!path.HasValue || path == "/")
            return true;

        foreach (var prefix in ExemptPathPrefixes)
        {
            if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static async Task WriteTenantProblemAsync(HttpContext context, string detail)
    {
        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Type = "https://httpstatuses.com/400",
            Title = "Tenant Required",
            Status = StatusCodes.Status400BadRequest,
            Detail = detail,
            Instance = context.Request.Path
        };

        problem.Extensions["correlationId"] = context.TraceIdentifier;

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problem, context.RequestAborted);
    }
}
