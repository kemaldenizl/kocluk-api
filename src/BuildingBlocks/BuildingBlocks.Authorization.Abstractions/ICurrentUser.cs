namespace BuildingBlocks.Authorization;

/// <summary>
/// Ambient accessor for the authenticated caller of the current request. Register with
/// <c>AddCurrentUser()</c>; resolves to <c>null</c> when the request is unauthenticated.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    CurrentUser? User { get; }
}
