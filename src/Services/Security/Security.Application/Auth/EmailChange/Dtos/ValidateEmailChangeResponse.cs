namespace Security.Application.Auth.EmailChange.Dtos;

public sealed record ValidateEmailChangeResponse(bool IsValid, string Message);
