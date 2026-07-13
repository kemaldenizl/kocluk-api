namespace Security.Domain.Abstractions;

/// <summary>
/// Marks an entity as belonging to a single tenant. The <see cref="TenantId"/> value is
/// assigned by the persistence layer at insert time (see the tenant save-changes interceptor),
/// so domain code and application handlers never have to pass it explicitly. Reads are isolated
/// automatically through EF Core global query filters.
/// </summary>
public interface ITenantScoped
{
    Guid TenantId { get; }
}
