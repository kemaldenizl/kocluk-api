using MassTransit;
using Microsoft.EntityFrameworkCore;
using DomainNotification = Notification.Domain.Notifications.Notification;

namespace Notification.Infrastructure.Persistence;

public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<DomainNotification> Notifications => Set<DomainNotification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("notification");

        builder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);

        // MassTransit transactional outbox + inbox. The inbox de-duplicates redelivered messages so each
        // integration event is consumed exactly once even if the broker redelivers.
        builder.AddInboxStateEntity();
        builder.AddOutboxMessageEntity();
        builder.AddOutboxStateEntity();
    }
}
