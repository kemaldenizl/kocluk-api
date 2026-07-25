namespace BuildingBlocks.Authentication;

/// <summary>
/// Well-known authorization policy names that are not permission codes. Referenced by name from
/// gateway route metadata (YARP <c>AuthorizationPolicy</c>) and endpoint <c>RequireAuthorization</c>.
/// </summary>
public static class AuthorizationPolicyNames
{
    /// <summary>Requires any authenticated caller. Used at the gateway edge.</summary>
    public const string Authenticated = "authenticated";
}
