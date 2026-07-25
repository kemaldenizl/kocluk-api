using MediatR;
using Security.Application.Common.Results;
using Security.Application.Memberships.Dtos;

namespace Security.Application.Memberships.GetMyMemberships;

/// <summary>Returns every membership of the current user across all tenants (history included).</summary>
public sealed record GetMyMembershipsQuery(
    Guid UserId
) : IRequest<Result<IReadOnlyList<MembershipDto>>>;
