namespace BuildingBlocks.Authorization.Validation;

/// <summary>Outcome of an <see cref="IAccessTokenValidator"/> check.</summary>
public sealed class TokenValidationResult
{
    private TokenValidationResult(bool isValid, string? failureReason)
    {
        IsValid = isValid;
        FailureReason = failureReason;
    }

    public bool IsValid { get; }

    public string? FailureReason { get; }

    public static TokenValidationResult Success() => new(true, null);

    public static TokenValidationResult Fail(string reason) => new(false, reason);
}
