using Security.Domain.Authorization;
using Security.Domain.Exceptions;

namespace UnitTests.Security.Domain;

public sealed class RoleTests
{
    [Fact]
    public void ForTenant_creates_tenant_scoped_role()
    {
        var tenantId = Guid.NewGuid();

        var role = Role.ForTenant(Guid.NewGuid(), tenantId, "Coach", "COACH");

        role.Scope.ShouldBe(RoleScope.Tenant);
        role.TenantId.ShouldBe(tenantId);
        role.Name.ShouldBe("Coach");
        role.Permissions.ShouldBeEmpty();
    }

    [Fact]
    public void ForTenant_rejects_empty_tenant()
        => Should.Throw<DomainException>(() => Role.ForTenant(Guid.NewGuid(), Guid.Empty, "Coach", "COACH"));

    [Fact]
    public void Platform_creates_tenant_independent_role()
    {
        var role = Role.Platform(Guid.NewGuid(), "SystemAdmin", "SYSTEMADMIN");

        role.Scope.ShouldBe(RoleScope.Platform);
        role.TenantId.ShouldBeNull();
    }

    [Fact]
    public void Constructor_rejects_blank_names()
    {
        Should.Throw<DomainException>(() => Role.Platform(Guid.NewGuid(), " ", "X"));
        Should.Throw<DomainException>(() => Role.Platform(Guid.NewGuid(), "X", " "));
    }

    [Fact]
    public void Rename_updates_name_and_normalized_name()
    {
        var role = RoleFactory.Tenant();

        role.Rename("Lead Coach", "LEAD COACH");

        role.Name.ShouldBe("Lead Coach");
        role.NormalizedName.ShouldBe("LEAD COACH");
    }

    [Fact]
    public void AddPermission_adds_once_and_is_idempotent()
    {
        var role = RoleFactory.Tenant();
        var permissionId = Guid.NewGuid();

        role.AddPermission(permissionId, TestClock.UtcNow);
        role.AddPermission(permissionId, TestClock.OneHourLater);

        role.Permissions.ShouldHaveSingleItem().PermissionId.ShouldBe(permissionId);
    }

    [Fact]
    public void AddPermission_rejects_empty_id_or_default_time()
    {
        var role = RoleFactory.Tenant();

        Should.Throw<DomainException>(() => role.AddPermission(Guid.Empty, TestClock.UtcNow));
        Should.Throw<DomainException>(() => role.AddPermission(Guid.NewGuid(), default));
    }

    [Fact]
    public void RemovePermission_removes_existing()
    {
        var role = RoleFactory.Tenant();
        var permissionId = Guid.NewGuid();
        role.AddPermission(permissionId, TestClock.UtcNow);

        role.RemovePermission(permissionId);

        role.Permissions.ShouldBeEmpty();
    }

    [Fact]
    public void RemovePermission_unknown_is_noop()
    {
        var role = RoleFactory.Tenant();
        role.AddPermission(Guid.NewGuid(), TestClock.UtcNow);

        role.RemovePermission(Guid.NewGuid());

        role.Permissions.Count.ShouldBe(1);
    }

    [Fact]
    public void RemovePermission_rejects_empty_id()
        => Should.Throw<DomainException>(() => RoleFactory.Tenant().RemovePermission(Guid.Empty));
}

public sealed class PermissionTests
{
    [Fact]
    public void Constructor_sets_code()
        => new Permission(Guid.NewGuid(), "users.read").Code.ShouldBe("users.read");

    [Fact]
    public void Constructor_rejects_empty_id_or_blank_code()
    {
        Should.Throw<DomainException>(() => new Permission(Guid.Empty, "users.read"));
        Should.Throw<DomainException>(() => new Permission(Guid.NewGuid(), " "));
    }

    [Fact]
    public void Rename_changes_code()
    {
        var permission = new Permission(Guid.NewGuid(), "users.read");

        permission.Rename("users.manage");

        permission.Code.ShouldBe("users.manage");
    }

    [Fact]
    public void Rename_rejects_blank()
        => Should.Throw<DomainException>(() => new Permission(Guid.NewGuid(), "x").Rename(" "));
}

public sealed class RolePermissionAndPlatformRoleTests
{
    [Fact]
    public void RolePermission_rejects_invalid_arguments()
    {
        Should.Throw<DomainException>(() => new RolePermission(Guid.Empty, Guid.NewGuid(), TestClock.UtcNow));
        Should.Throw<DomainException>(() => new RolePermission(Guid.NewGuid(), Guid.Empty, TestClock.UtcNow));
        Should.Throw<DomainException>(() => new RolePermission(Guid.NewGuid(), Guid.NewGuid(), default));
    }

    [Fact]
    public void UserPlatformRole_rejects_invalid_arguments()
    {
        Should.Throw<DomainException>(() => new UserPlatformRole(Guid.Empty, Guid.NewGuid(), TestClock.UtcNow));
        Should.Throw<DomainException>(() => new UserPlatformRole(Guid.NewGuid(), Guid.Empty, TestClock.UtcNow));
        Should.Throw<DomainException>(() => new UserPlatformRole(Guid.NewGuid(), Guid.NewGuid(), default));
    }

    [Fact]
    public void UserPlatformRole_stores_values()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var sut = new UserPlatformRole(userId, roleId, TestClock.UtcNow);

        sut.UserId.ShouldBe(userId);
        sut.RoleId.ShouldBe(roleId);
        sut.AssignedAtUtc.ShouldBe(TestClock.UtcNow);
    }
}
