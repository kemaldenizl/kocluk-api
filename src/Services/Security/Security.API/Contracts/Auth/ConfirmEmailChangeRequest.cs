namespace Security.API.Contracts.Auth;

public sealed record ConfirmEmailChangeRequest(string Token, string NewEmail);
