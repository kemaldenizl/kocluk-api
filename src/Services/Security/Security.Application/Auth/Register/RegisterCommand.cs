using MediatR;
using Security.Application.Common.Results;
using Security.Domain.Users;

namespace Security.Application.Auth.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string ConfirmPassword,
    string FirstName,
    string LastName,
    StudentType StudentType
) : IRequest<Result<RegisterResponse>>;
