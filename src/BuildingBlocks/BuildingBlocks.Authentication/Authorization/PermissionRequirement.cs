using Microsoft.AspNetCore.Authorization;

namespace BuildingBlocks.Authentication;

/// <summary>Requires the caller's token to carry a specific permission code.</summary>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
