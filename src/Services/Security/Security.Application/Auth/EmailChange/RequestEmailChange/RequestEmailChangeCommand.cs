using MediatR;
using Security.Application.Auth.EmailChange.Dtos;
using Security.Application.Common.Results;

namespace Security.Application.Auth.EmailChange.RequestEmailChange;

public sealed record RequestEmailChangeCommand(Guid UserId, string CurrentPassword) : IRequest<Result<RequestEmailChangeResponse>>;
