using Security.Application.Memberships.Dtos;

namespace Security.Application.Abstractions.Memberships;

/// <summary>
/// Read-side service for memberships: assembles a user's <see cref="UserAccessContext"/> for token
/// issuance and projects membership lists (a user's own memberships, a tenant's members).
/// </summary>
public interface IMembershipQueryService
{
    Task<UserAccessContext> GetAccessContextAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>All memberships of a user across every tenant (history included).</summary>
    Task<IReadOnlyList<MembershipDto>> GetUserMembershipsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>All members of a tenant, projected with their user identity.</summary>
    Task<IReadOnlyList<TenantMemberDto>> GetTenantMembersAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
