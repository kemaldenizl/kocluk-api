using MediatR;
using Security.Application.Common.Results;
using Security.Application.Memberships.Dtos;

namespace Security.Application.Memberships.AddMember;

/// <summary>
/// Adds an existing platform user to a tenant as a member (with optional tenant roles). The target user
/// is identified by <see cref="UserId"/> or, when that is null, by <see cref="Email"/>.
/// </summary>
public sealed record AddMemberCommand(
    Guid TenantId,
    Guid CallerUserId,
    Guid? UserId,
    string? Email,
    IReadOnlyList<Guid> RoleIds
) : IRequest<Result<MembershipDto>>;
