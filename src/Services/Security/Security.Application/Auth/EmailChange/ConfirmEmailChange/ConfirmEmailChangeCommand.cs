using MediatR;
using Security.Application.Common.Results;

namespace Security.Application.Auth.EmailChange.ConfirmEmailChange;

public sealed record ConfirmEmailChangeCommand(string Token, string NewEmail) : IRequest<Result>;
