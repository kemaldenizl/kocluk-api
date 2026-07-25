using Tenant.Domain.Exceptions;
using DomainTenant = Tenant.Domain.Tenants.Tenant;

namespace UnitTests.Tenant;

public sealed class TenantDomainTests
{
    private static DomainTenant Create(string name = "Acme Inc.", string slug = "acme-inc")
        => new(Guid.NewGuid(), name, slug, TestClock.UtcNow);

    [Fact]
    public void Constructor_sets_defaults_and_stamps()
    {
        var tenant = Create();

        tenant.IsActive.ShouldBeTrue();
        tenant.UpdatedAtUtc.ShouldBe(TestClock.UtcNow);
    }

    [Fact]
    public void Constructor_rejects_invalid_arguments()
    {
        Should.Throw<DomainException>(() => new DomainTenant(Guid.Empty, "Acme", "acme", TestClock.UtcNow));
        Should.Throw<DomainException>(() => Create(name: " "));
        Should.Throw<DomainException>(() => Create(slug: " "));
        Should.Throw<DomainException>(() => new DomainTenant(Guid.NewGuid(), "Acme", "acme", default));
    }

    [Fact]
    public void Rename_changes_name_and_touches_and_returns_true()
    {
        var tenant = Create(name: "Acme Inc.");

        var changed = tenant.Rename("Acme International", TestClock.OneHourLater);

        changed.ShouldBeTrue();
        tenant.Name.ShouldBe("Acme International");
        tenant.UpdatedAtUtc.ShouldBe(TestClock.OneHourLater);
    }

    [Fact]
    public void Rename_to_same_name_returns_false_and_does_not_touch()
    {
        var tenant = Create(name: "Acme Inc.");

        var changed = tenant.Rename("Acme Inc.", TestClock.OneHourLater);

        changed.ShouldBeFalse();
        tenant.UpdatedAtUtc.ShouldBe(TestClock.UtcNow);
    }

    [Fact]
    public void Rename_rejects_blank()
        => Should.Throw<DomainException>(() => Create().Rename(" ", TestClock.OneHourLater));

    [Fact]
    public void Deactivate_then_activate_report_state_changes()
    {
        var tenant = Create();

        tenant.Deactivate(TestClock.OneHourLater).ShouldBeTrue();
        tenant.IsActive.ShouldBeFalse();
        tenant.UpdatedAtUtc.ShouldBe(TestClock.OneHourLater);

        // Already inactive -> no change.
        tenant.Deactivate(TestClock.OneDayLater).ShouldBeFalse();
        tenant.UpdatedAtUtc.ShouldBe(TestClock.OneHourLater);

        tenant.Activate(TestClock.OneDayLater).ShouldBeTrue();
        tenant.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Activate_when_already_active_returns_false()
        => Create().Activate(TestClock.OneHourLater).ShouldBeFalse();
}
