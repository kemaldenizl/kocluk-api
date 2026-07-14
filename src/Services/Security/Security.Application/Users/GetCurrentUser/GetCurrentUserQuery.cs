using MediatR;
using Security.Application.Common.Results;

namespace Security.Application.Users.GetCurrentUser;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<Result<CurrentUserProfileDto>>;
