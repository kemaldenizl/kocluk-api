using Security.Domain.Auditing;
using Security.Domain.Exceptions;
using SecurityTenant = Security.Domain.Tenancy.Tenant;

namespace UnitTests.Security.Domain;

public sealed class AuditLogTests
{
    private static AuditLog Create(Guid? id = null, Guid? userId = null, string ip = "203.0.113.4",
        string agent = "agent", string correlationId = "corr", string payload = "{}")
        => new(id ?? Guid.NewGuid(), userId, AuditActionType.LoginSucceeded, ip, agent, correlationId, payload, TestClock.UtcNow);

    [Fact]
    public void Constructor_stores_all_fields()
    {
        var userId = Guid.NewGuid();

        var log = Create(userId: userId);

        log.UserId.ShouldBe(userId);
        log.ActionType.ShouldBe(AuditActionType.LoginSucceeded);
        log.CreatedAtUtc.ShouldBe(TestClock.UtcNow);
    }

    [Fact]
    public void UserId_may_be_null_for_anonymous_actions()
        => Create(userId: null).UserId.ShouldBeNull();

    [Fact]
    public void Constructor_rejects_empty_id_and_blank_required_strings()
    {
        Should.Throw<DomainException>(() => Create(id: Guid.Empty));
        Should.Throw<DomainException>(() => Create(ip: " "));
        Should.Throw<DomainException>(() => Create(agent: " "));
        Should.Throw<DomainException>(() => Create(correlationId: " "));
        Should.Throw<DomainException>(() => Create(payload: " "));
    }
}

public sealed class SecurityTenantReadModelTests
{
    private static SecurityTenant Create(string name = "Acme", string slug = "acme")
        => new(Guid.NewGuid(), name, slug, TestClock.UtcNow);

    [Fact]
    public void Constructor_starts_active()
        => Create().IsActive.ShouldBeTrue();

    [Fact]
    public void Constructor_rejects_blank_name_or_slug()
    {
        Should.Throw<DomainException>(() => Create(name: " "));
        Should.Throw<DomainException>(() => Create(slug: " "));
    }

    [Fact]
    public void Rename_updates_name()
    {
        var tenant = Create();

        tenant.Rename("New Name");

        tenant.Name.ShouldBe("New Name");
    }

    [Fact]
    public void Activate_and_Deactivate_toggle_state()
    {
        var tenant = Create();

        tenant.Deactivate();
        tenant.IsActive.ShouldBeFalse();

        tenant.Activate();
        tenant.IsActive.ShouldBeTrue();
    }
}
