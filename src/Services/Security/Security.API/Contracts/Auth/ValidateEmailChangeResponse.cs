namespace Security.API.Contracts.Auth;

public sealed record ValidateEmailChangeResponse(bool IsValid, string Message);
