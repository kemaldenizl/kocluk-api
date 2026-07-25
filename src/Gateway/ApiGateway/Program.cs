using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Structured logging, kept consistent with the downstream services.
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "ApiGateway")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Yarp", LogEventLevel.Information);
});

// YARP reverse proxy. Routes and clusters are declared in configuration so that
// destinations can be overridden per environment (localhost for local dev, service
// names inside Docker/Kubernetes) without touching code.
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Edge authentication: validate the Security-issued JWT once at the gateway and reject anonymous
// traffic except the routes explicitly marked with the "anonymous" policy (login/register/refresh).
// Downstream services re-validate independently, so this is defense in depth, not the only gate.
builder.Services.AddGatewayAuthentication(builder.Configuration);

// CORS so the browser-based frontend can call the gateway directly. Origins are
// configurable; when none are supplied every origin is allowed (development only).
const string CorsPolicy = "gateway-cors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        if (allowedOrigins.Length == 0)
        {
            policy
                .SetIsOriginAllowed(_ => true)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
        else
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("Gateway is running"), tags: ["live"]);

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseCors(CorsPolicy);

// Must run before MapReverseProxy so YARP honors each route's AuthorizationPolicy metadata.
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true
});

app.MapGet("/", () => Results.Ok(new
{
    service = "ApiGateway",
    status = "running"
}));

// All /api/** traffic is matched by the reverse-proxy routes and forwarded to the owning
// microservice. Each route declares an AuthorizationPolicy in configuration ("anonymous" for the
// public auth endpoints, "authenticated" for everything else). The Authorization header is passed
// through untouched so downstream services can re-validate and enforce fine-grained permissions.
app.MapReverseProxy();

app.Run();
