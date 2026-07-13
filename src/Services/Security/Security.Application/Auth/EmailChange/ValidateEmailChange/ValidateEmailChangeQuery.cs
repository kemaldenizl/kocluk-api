using MediatR;
using Security.Application.Auth.EmailChange.Dtos;
using Security.Application.Common.Results;

namespace Security.Application.Auth.EmailChange.ValidateEmailChange;

public sealed record ValidateEmailChangeQuery(string Token) : IRequest<Result<ValidateEmailChangeResponse>>;
