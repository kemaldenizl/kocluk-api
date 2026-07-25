using MediatR;
using Security.Application.Common.Results;
using Security.Application.Memberships.Dtos;

namespace Security.Application.Memberships.GetTenantMembers;

/// <summary>Returns all members of a tenant. Caller must have members.read (or be a system admin).</summary>
public sealed record GetTenantMembersQuery(
    Guid TenantId,
    Guid CallerUserId
) : IRequest<Result<IReadOnlyList<TenantMemberDto>>>;
