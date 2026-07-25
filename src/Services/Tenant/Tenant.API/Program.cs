using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;
using Tenant.API.Common.ErrorMapping;
using Tenant.API.Endpoints;
using Tenant.API.Extensions;
using Tenant.API.HealthChecks;
using Tenant.Application;
using Tenant.Infrastructure;
using Tenant.Infrastructure.Persistence;
using Tenant.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Tenant.API")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning);
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddTenantAuthentication(builder.Configuration);

builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API is running"), tags: ["live"])
    .AddDbContextCheck<TenantDbContext>(name: "postgresql", failureStatus: HealthStatus.Unhealthy, tags: ["ready", "db"]);

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();

    if (app.Environment.IsDevelopment()
        || app.Environment.IsStaging()
        || app.Environment.IsEnvironment("IntegrationTests"))
    {
        await dbContext.Database.MigrateAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<TenantSeeder>();
        await seeder.SeedAsync();
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
    ResponseWriter = HealthCheckResponseWriter.WriteResponseAsync
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteResponseAsync
});

app.MapGet("/", () => Results.Ok(new
{
    service = "Tenant.API",
    status = "running"
}));

app.MapTenantEndpoints();

app.Run();

public partial class Program;
