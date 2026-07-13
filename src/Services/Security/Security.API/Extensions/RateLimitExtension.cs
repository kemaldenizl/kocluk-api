using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Security.Infrastructure.RateLimiting;
using Security.API.Abstractions;

namespace Security.API.Extensions;

public static class RateLimitExtension
{
    public static IServiceCollection AddRateLimitExt(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RateLimitOptions>(configuration.GetSection(RateLimitOptions.SectionName));
        var rateLimitOptions = configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>() ?? new RateLimitOptions();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/problem+json";

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);                      
                }

                var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Type = "https://httpstatuses.com/429",
                    Title = "Too Many Requests",
                    Status = StatusCodes.Status429TooManyRequests,
                    Detail = "Rate limit exceeded. Please try again later.",
                    Instance = context.HttpContext.Request.Path
                };

                problem.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;

                await context.HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            };

            options.AddPolicy(RateLimitPolicyNames.Register, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ByIp(httpContext, "register"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.Register.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.Register.WindowSeconds),
                        QueueLimit = rateLimitOptions.Register.QueueLimit,
                        AutoReplenishment = rateLimitOptions.Register.AutoReplenishment
                    }));

            options.AddPolicy(RateLimitPolicyNames.Login, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ByIp(httpContext, "login"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.Login.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.Login.WindowSeconds),
                        QueueLimit = rateLimitOptions.Login.QueueLimit,
                        AutoReplenishment = rateLimitOptions.Login.AutoReplenishment
                    }));

            options.AddPolicy(RateLimitPolicyNames.Refresh, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ByIp(httpContext, "refresh"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.Refresh.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.Refresh.WindowSeconds),
                        QueueLimit = rateLimitOptions.Refresh.QueueLimit,
                        AutoReplenishment = rateLimitOptions.Refresh.AutoReplenishment
                    }));

            options.AddPolicy(RateLimitPolicyNames.Logout, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ByAuthenticatedUserOrIp(httpContext, "logout"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.Logout.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.Logout.WindowSeconds),
                        QueueLimit = rateLimitOptions.Logout.QueueLimit,
                        AutoReplenishment = rateLimitOptions.Logout.AutoReplenishment
                    }));

            options.AddPolicy(RateLimitPolicyNames.Sessions, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ByAuthenticatedUserOrIp(httpContext, "sessions"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.Sessions.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.Sessions.WindowSeconds),
                        QueueLimit = rateLimitOptions.Sessions.QueueLimit,
                        AutoReplenishment = rateLimitOptions.Sessions.AutoReplenishment
                    }));

            options.AddPolicy(RateLimitPolicyNames.ForgotPassword, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ByIp(httpContext, "forgot-password"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.ForgotPassword.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.ForgotPassword.WindowSeconds),
                        QueueLimit = rateLimitOptions.ForgotPassword.QueueLimit,
                        AutoReplenishment = rateLimitOptions.ForgotPassword.AutoReplenishment
                    }));

            options.AddPolicy(RateLimitPolicyNames.ResetPassword, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ByIp(httpContext, "reset-password"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.ResetPassword.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.ResetPassword.WindowSeconds),
                        QueueLimit = rateLimitOptions.ResetPassword.QueueLimit,
                        AutoReplenishment = rateLimitOptions.ResetPassword.AutoReplenishment
                    }));
            
            options.AddPolicy(RateLimitPolicyNames.ChangePasswordRequest, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ByAuthenticatedUserOrIp(httpContext, "change-password-request"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.ChangePasswordRequest.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.ChangePasswordRequest.WindowSeconds),
                        QueueLimit = rateLimitOptions.ChangePasswordRequest.QueueLimit,
                        AutoReplenishment = rateLimitOptions.ChangePasswordRequest.AutoReplenishment
                    }));

            options.AddPolicy(RateLimitPolicyNames.ChangePasswordConfirm, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ByIp(httpContext, "change-password-confirm"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.ChangePasswordConfirm.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.ChangePasswordConfirm.WindowSeconds),
                        QueueLimit = rateLimitOptions.ChangePasswordConfirm.QueueLimit,
                        AutoReplenishment = rateLimitOptions.ChangePasswordConfirm.AutoReplenishment
                    }));

            options.AddPolicy(RateLimitPolicyNames.ChangeEmailRequest, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ByAuthenticatedUserOrIp(httpContext, "change-email-request"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.ChangeEmailRequest.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.ChangeEmailRequest.WindowSeconds),
                        QueueLimit = rateLimitOptions.ChangeEmailRequest.QueueLimit,
                        AutoReplenishment = rateLimitOptions.ChangeEmailRequest.AutoReplenishment
                    }));

            options.AddPolicy(RateLimitPolicyNames.ChangeEmailValidate, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ByIp(httpContext, "change-email-validate"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.ChangeEmailValidate.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.ChangeEmailValidate.WindowSeconds),
                        QueueLimit = rateLimitOptions.ChangeEmailValidate.QueueLimit,
                        AutoReplenishment = rateLimitOptions.ChangeEmailValidate.AutoReplenishment
                    }));

            options.AddPolicy(RateLimitPolicyNames.ChangeEmailConfirm, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ByIp(httpContext, "change-email-confirm"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.ChangeEmailConfirm.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.ChangeEmailConfirm.WindowSeconds),
                        QueueLimit = rateLimitOptions.ChangeEmailConfirm.QueueLimit,
                        AutoReplenishment = rateLimitOptions.ChangeEmailConfirm.AutoReplenishment
                    }));

            options.AddPolicy(RateLimitPolicyNames.VerifyEmail, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ByIp(httpContext, "verify-email"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.VerifyEmail.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.VerifyEmail.WindowSeconds),
                        QueueLimit = rateLimitOptions.VerifyEmail.QueueLimit,
                        AutoReplenishment = rateLimitOptions.VerifyEmail.AutoReplenishment
                    }));

            options.AddPolicy(RateLimitPolicyNames.ResendVerification, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ByIp(httpContext, "resend-verification"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.ResendVerification.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.ResendVerification.WindowSeconds),
                        QueueLimit = rateLimitOptions.ResendVerification.QueueLimit,
                        AutoReplenishment = rateLimitOptions.ResendVerification.AutoReplenishment
                    }));

            options.AddPolicy(RateLimitPolicyNames.Admin, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ByAuthenticatedUserOrIp(httpContext, "admin"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.Admin.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.Admin.WindowSeconds),
                        QueueLimit = rateLimitOptions.Admin.QueueLimit,
                        AutoReplenishment = rateLimitOptions.Admin.AutoReplenishment
                    }));
        });

        return services;
    }
}