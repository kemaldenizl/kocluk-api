using MediatR;
using Security.Application.Abstractions.Memberships;
using Security.Application.Common.Results;
using Security.Application.Memberships.Dtos;

namespace Security.Application.Memberships.GetMyMemberships;

public sealed class GetMyMembershipsQueryHandler(IMembershipQueryService membershipQueryService)
    : IRequestHandler<GetMyMembershipsQuery, Result<IReadOnlyList<MembershipDto>>>
{
    public async Task<Result<IReadOnlyList<MembershipDto>>> Handle(
        GetMyMembershipsQuery request,
        CancellationToken cancellationToken)
    {
        var memberships = await membershipQueryService.GetUserMembershipsAsync(request.UserId, cancellationToken);
        return Result<IReadOnlyList<MembershipDto>>.Success(memberships);
    }
}
