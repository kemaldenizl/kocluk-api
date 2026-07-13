using MediatR;
using Security.Application.Auth.PasswordChange.Dtos;
using Security.Application.Common.Results;

namespace Security.Application.Auth.PasswordChange.RequestPasswordChange;

public sealed record RequestPasswordChangeCommand(Guid UserId, string CurrentPassword) : IRequest<Result<RequestPasswordChangeResponse>>;
