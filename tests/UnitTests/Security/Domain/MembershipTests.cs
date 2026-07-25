using Security.Domain.Exceptions;
using Security.Domain.Memberships;

namespace UnitTests.Security.Domain;

public sealed class MembershipTests
{
    private static Membership Create() => MembershipBuilder.Default();

    [Fact]
    public void Constructor_starts_active_and_stamps_timestamps()
    {
        var membership = new MembershipBuilder().WithJoinedAt(TestClock.UtcNow).Build();

        membership.Status.ShouldBe(MembershipStatus.Active);
        membership.IsActive.ShouldBeTrue();
        membership.JoinedAtUtc.ShouldBe(TestClock.UtcNow);
        membership.UpdatedAtUtc.ShouldBe(TestClock.UtcNow);
        membership.Roles.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_rejects_empty_ids_and_default_time()
    {
        Should.Throw<DomainException>(() => new Membership(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), TestClock.UtcNow));
        Should.Throw<DomainException>(() => new Membership(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), TestClock.UtcNow));
        Should.Throw<DomainException>(() => new Membership(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, TestClock.UtcNow));
        Should.Throw<DomainException>(() => new Membership(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), default));
    }

    [Fact]
    public void AddRole_adds_and_touches()
    {
        var membership = Create();
        var roleId = Guid.NewGuid();

        membership.AddRole(roleId, TestClock.OneHourLater);

        membership.Roles.ShouldHaveSingleItem().RoleId.ShouldBe(roleId);
        membership.UpdatedAtUtc.ShouldBe(TestClock.OneHourLater);
    }

    [Fact]
    public void AddRole_is_idempotent_for_same_role()
    {
        var membership = Create();
        var roleId = Guid.NewGuid();

        membership.AddRole(roleId, TestClock.UtcNow);
        var stampAfterFirst = membership.UpdatedAtUtc;
        membership.AddRole(roleId, TestClock.OneHourLater);

        membership.Roles.Count.ShouldBe(1);
        membership.UpdatedAtUtc.ShouldBe(stampAfterFirst); // second add is a no-op, does not touch
    }

    [Fact]
    public void AddRole_rejects_empty_role_or_default_time()
    {
        var membership = Create();

        Should.Throw<DomainException>(() => membership.AddRole(Guid.Empty, TestClock.UtcNow));
        Should.Throw<DomainException>(() => membership.AddRole(Guid.NewGuid(), default));
    }

    [Fact]
    public void RemoveRole_removes_existing_and_touches()
    {
        var membership = Create();
        var roleId = Guid.NewGuid();
        membership.AddRole(roleId, TestClock.UtcNow);

        membership.RemoveRole(roleId, TestClock.OneHourLater);

        membership.Roles.ShouldBeEmpty();
        membership.UpdatedAtUtc.ShouldBe(TestClock.OneHourLater);
    }

    [Fact]
    public void RemoveRole_unknown_role_is_noop()
    {
        var membership = Create();
        var stamp = membership.UpdatedAtUtc;

        membership.RemoveRole(Guid.NewGuid(), TestClock.OneHourLater);

        membership.UpdatedAtUtc.ShouldBe(stamp);
    }

    [Fact]
    public void Suspend_moves_to_suspended()
    {
        var membership = Create();

        membership.Suspend(TestClock.OneHourLater);

        membership.Status.ShouldBe(MembershipStatus.Suspended);
        membership.IsActive.ShouldBeFalse();
        membership.UpdatedAtUtc.ShouldBe(TestClock.OneHourLater);
    }

    [Fact]
    public void Suspend_is_idempotent()
    {
        var membership = Create();
        membership.Suspend(TestClock.UtcNow);
        var stamp = membership.UpdatedAtUtc;

        membership.Suspend(TestClock.OneHourLater);

        membership.UpdatedAtUtc.ShouldBe(stamp);
    }

    [Fact]
    public void Remove_soft_removes_but_keeps_roles()
    {
        var membership = Create();
        membership.AddRole(Guid.NewGuid(), TestClock.UtcNow);

        membership.Remove(TestClock.OneHourLater);

        membership.Status.ShouldBe(MembershipStatus.Removed);
        membership.Roles.Count.ShouldBe(1);
    }

    [Fact]
    public void Reactivate_restores_active_from_suspended_or_removed()
    {
        var suspended = Create();
        suspended.Suspend(TestClock.UtcNow);
        suspended.Reactivate(TestClock.OneHourLater);
        suspended.Status.ShouldBe(MembershipStatus.Active);

        var removed = Create();
        removed.Remove(TestClock.UtcNow);
        removed.Reactivate(TestClock.OneHourLater);
        removed.Status.ShouldBe(MembershipStatus.Active);
    }

    [Fact]
    public void Reactivate_when_already_active_is_noop()
    {
        var membership = Create();
        var stamp = membership.UpdatedAtUtc;

        membership.Reactivate(TestClock.OneHourLater);

        membership.UpdatedAtUtc.ShouldBe(stamp);
    }
}
