namespace Security.API.Contracts.Auth;

public sealed record ConfirmPasswordChangeRequest(string Token, string NewPassword);
