using System.Reflection;
using NetArchTest.Rules;

namespace ArchitectureTests;

/// <summary>
/// Enforces the Clean Architecture dependency rule for every microservice: dependencies point inward only
/// (API → Infrastructure → Application → Domain), and the inner layers stay free of outer-layer frameworks.
/// These run as ordinary unit tests so a violating reference fails the build instead of silently eroding
/// the architecture.
/// </summary>
public sealed class LayeringTests
{
    public sealed record Service(string Name, Assembly Domain, Assembly Application)
    {
        public string DomainNs => $"{Name}.Domain";
        public string ApplicationNs => $"{Name}.Application";
        public string InfrastructureNs => $"{Name}.Infrastructure";
        public string ApiNs => $"{Name}.API";
    }

    public static IEnumerable<object[]> Services()
    {
        yield return [new Service("Security",
            typeof(global::Security.Domain.Users.User).Assembly,
            typeof(global::Security.Application.AssemblyMarker).Assembly)];
        yield return [new Service("Tenant",
            typeof(global::Tenant.Domain.Tenants.Tenant).Assembly,
            typeof(global::Tenant.Application.AssemblyMarker).Assembly)];
        yield return [new Service("Notification",
            typeof(global::Notification.Domain.Notifications.Notification).Assembly,
            typeof(global::Notification.Application.AssemblyMarker).Assembly)];
    }

    private static readonly string[] OuterFrameworks =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Npgsql"
    ];

    [Theory]
    [MemberData(nameof(Services))]
    public void Domain_does_not_depend_on_outer_layers(Service service)
    {
        var result = Types.InAssembly(service.Domain)
            .That().ResideInNamespaceStartingWith(service.DomainNs)
            .ShouldNot().HaveDependencyOnAny(service.ApplicationNs, service.InfrastructureNs, service.ApiNs)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result, $"{service.DomainNs} must not reference outer layers"));
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Domain_is_free_of_infrastructure_frameworks(Service service)
    {
        var result = Types.InAssembly(service.Domain)
            .That().ResideInNamespaceStartingWith(service.DomainNs)
            .ShouldNot().HaveDependencyOnAny([.. OuterFrameworks, "MediatR", "FluentValidation"])
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result, $"{service.DomainNs} must stay persistence/web/mediator agnostic"));
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Application_does_not_depend_on_infrastructure_or_api(Service service)
    {
        var result = Types.InAssembly(service.Application)
            .That().ResideInNamespaceStartingWith(service.ApplicationNs)
            .ShouldNot().HaveDependencyOnAny(service.InfrastructureNs, service.ApiNs)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result, $"{service.ApplicationNs} must not reference Infrastructure/API"));
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Application_does_not_depend_on_persistence_or_web_frameworks(Service service)
    {
        var result = Types.InAssembly(service.Application)
            .That().ResideInNamespaceStartingWith(service.ApplicationNs)
            .ShouldNot().HaveDependencyOnAny(OuterFrameworks)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result, $"{service.ApplicationNs} must not reference EF Core/ASP.NET/Npgsql"));
    }

    private static string Describe(TestResult result, string rule)
    {
        var offenders = result.FailingTypeNames is null ? [] : result.FailingTypeNames.ToArray();
        return offenders.Length == 0
            ? rule
            : $"{rule}. Offending types: {string.Join(", ", offenders)}";
    }
}
