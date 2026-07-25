using MediatR;
using Security.Application.Abstractions.Memberships;
using Security.Application.Common.Errors;
using Security.Application.Common.Results;
using Security.Application.Memberships.Dtos;
using Security.Domain.Authorization;

namespace Security.Application.Memberships.GetTenantMembers;

public sealed class GetTenantMembersQueryHandler(
    IMembershipQueryService membershipQueryService,
    ITenantAccessAuthorizer tenantAccessAuthorizer)
    : IRequestHandler<GetTenantMembersQuery, Result<IReadOnlyList<TenantMemberDto>>>
{
    public async Task<Result<IReadOnlyList<TenantMemberDto>>> Handle(
        GetTenantMembersQuery request,
        CancellationToken cancellationToken)
    {
        var authorized = await tenantAccessAuthorizer.HasTenantPermissionAsync(
            request.CallerUserId, request.TenantId, PermissionCodes.MembersRead, cancellationToken);
        if (!authorized)
            return Result<IReadOnlyList<TenantMemberDto>>.Failure(AccessErrors.Forbidden);

        var members = await membershipQueryService.GetTenantMembersAsync(request.TenantId, cancellationToken);
        return Result<IReadOnlyList<TenantMemberDto>>.Success(members);
    }
}
