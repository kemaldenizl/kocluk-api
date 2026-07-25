using BuildingBlocks.Domain;

namespace UnitTests.BuildingBlocks;

public sealed class EntityTests
{
    private sealed record SampleEvent(DateTime OccurredOnUtc) : IDomainEvent;

    private sealed class SampleEntity : Entity
    {
        public void Raise(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
    }

    [Fact]
    public void New_entity_has_no_domain_events()
    {
        var entity = new SampleEntity();

        entity.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void RaiseDomainEvent_appends_event_in_order()
    {
        var entity = new SampleEntity();
        var first = new SampleEvent(TestClock.UtcNow);
        var second = new SampleEvent(TestClock.OneHourLater);

        entity.Raise(first);
        entity.Raise(second);

        entity.DomainEvents.ShouldBe([first, second]);
    }

    [Fact]
    public void ClearDomainEvents_removes_all_events()
    {
        var entity = new SampleEntity();
        entity.Raise(new SampleEvent(TestClock.UtcNow));

        entity.ClearDomainEvents();

        entity.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void DomainEvents_is_a_readonly_snapshot()
    {
        var entity = new SampleEntity();
        entity.Raise(new SampleEvent(TestClock.UtcNow));

        // The exposed collection must not allow external mutation.
        entity.DomainEvents.ShouldBeAssignableTo<IReadOnlyCollection<IDomainEvent>>();
    }
}
