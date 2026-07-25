using BuildingBlocks.Authorization.Validation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Authentication;

/// <summary>
/// Builds the shared <see cref="JwtBearerEvents"/>: an optional post-signature revocation hook
/// (<see cref="IAccessTokenValidator"/>, skipped when none is registered) plus RFC 7807
/// <c>application/problem+json</c> responses for 401/403 so every service replies identically.
/// </summary>
internal static class ProblemDetailsJwtBearerEvents
{
    public static JwtBearerEvents Create() => new()
    {
        OnTokenValidated = async context =>
        {
            var validator = context.HttpContext.RequestServices.GetService<IAccessTokenValidator>();
            if (validator is null)
                return;

            var principal = context.Principal;
            if (principal is null)
            {
                context.Fail("Token principal is missing.");
                return;
            }

            var result = await validator.ValidateAsync(principal, context.HttpContext.RequestAborted);
            if (!result.IsValid)
                context.Fail(result.FailureReason ?? "Token validation failed.");
        },

        OnChallenge = async context =>
        {
            if (context.Response.HasStarted)
                return;

            context.HandleResponse();

            var problem = new ProblemDetails
            {
                Type = "https://httpstatuses.com/401",
                Title = "Unauthorized",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Authentication is required or the access token is invalid.",
                Instance = context.HttpContext.Request.Path
            };

            problem.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(problem, context.HttpContext.RequestAborted);
        },

        OnForbidden = async context =>
        {
            var problem = new ProblemDetails
            {
                Type = "https://httpstatuses.com/403",
                Title = "Forbidden",
                Status = StatusCodes.Status403Forbidden,
                Detail = "You do not have permission to access this resource.",
                Instance = context.HttpContext.Request.Path
            };

            problem.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(problem, context.HttpContext.RequestAborted);
        }
    };
}
