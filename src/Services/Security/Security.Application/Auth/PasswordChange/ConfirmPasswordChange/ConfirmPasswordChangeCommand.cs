using MediatR;
using Security.Application.Common.Results;

namespace Security.Application.Auth.PasswordChange.ConfirmPasswordChange;

public sealed record ConfirmPasswordChangeCommand(string Token, string NewPassword) : IRequest<Result>;
