using BuildingBlocks.Authorization;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Authentication;

/// <summary>
/// <see cref="ICurrentUser"/> backed by the ambient <see cref="HttpContext"/>. Returns a projected
/// <see cref="CurrentUser"/> for authenticated requests and <c>null</c> otherwise.
/// </summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public CurrentUser? User =>
        IsAuthenticated ? httpContextAccessor.HttpContext!.User.ToCurrentUser() : null;
}
