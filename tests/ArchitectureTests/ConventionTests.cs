using System.Reflection;
using BuildingBlocks.Domain;
using NetArchTest.Rules;

namespace ArchitectureTests;

/// <summary>
/// Coding conventions that keep the domain model and CQRS pipeline predictable: aggregates are sealed
/// (no accidental inheritance of entity identity), and every FluentValidation validator is a sealed type
/// that lives in its service's Application layer.
/// </summary>
public sealed class ConventionTests
{
    public static IEnumerable<object[]> DomainAssemblies()
    {
        yield return [typeof(global::Security.Domain.Users.User).Assembly, "Security.Domain"];
        yield return [typeof(global::Tenant.Domain.Tenants.Tenant).Assembly, "Tenant.Domain"];
        yield return [typeof(global::Notification.Domain.Notifications.Notification).Assembly, "Notification.Domain"];
    }

    public static IEnumerable<object[]> ApplicationAssemblies()
    {
        yield return [typeof(global::Security.Application.AssemblyMarker).Assembly, "Security.Application"];
        yield return [typeof(global::Tenant.Application.AssemblyMarker).Assembly, "Tenant.Application"];
        yield return [typeof(global::Notification.Application.AssemblyMarker).Assembly, "Notification.Application"];
    }

    [Theory]
    [MemberData(nameof(DomainAssemblies))]
    public void Aggregate_roots_are_sealed(Assembly domain, string name)
    {
        var result = Types.InAssembly(domain)
            .That().Inherit(typeof(AggregateRoot))
            .Should().BeSealed()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue($"{name}: all aggregate roots must be sealed. Offenders: {Offenders(result)}");
    }

    [Theory]
    [MemberData(nameof(ApplicationAssemblies))]
    public void Validators_are_sealed(Assembly application, string name)
    {
        var result = Types.InAssembly(application)
            .That().HaveNameEndingWith("Validator")
            .Should().BeSealed()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue($"{name}: all validators must be sealed. Offenders: {Offenders(result)}");
    }

    [Theory]
    [MemberData(nameof(ApplicationAssemblies))]
    public void Validators_live_in_the_application_layer(Assembly application, string name)
    {
        var result = Types.InAssembly(application)
            .That().HaveNameEndingWith("Validator")
            .Should().ResideInNamespaceStartingWith($"{name}")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue($"{name}: validators must reside under the Application namespace. Offenders: {Offenders(result)}");
    }

    private static string Offenders(TestResult result)
        => result.FailingTypeNames is null ? "none" : string.Join(", ", result.FailingTypeNames);
}
