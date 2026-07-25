using System.Security.Claims;
using BuildingBlocks.Authentication;
using BuildingBlocks.Authorization;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace UnitTests.BuildingBlocks;

public sealed class HttpContextCurrentUserTests
{
    private static IHttpContextAccessor AccessorFor(HttpContext? context)
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(context);
        return accessor;
    }

    private static HttpContext AuthenticatedContext()
    {
        var claims = new[] { new Claim(CustomClaimTypes.Subject, Guid.NewGuid().ToString()) };
        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"))
        };
    }

    [Fact]
    public void IsAuthenticated_false_when_no_http_context()
    {
        var sut = new HttpContextCurrentUser(AccessorFor(null));

        sut.IsAuthenticated.ShouldBeFalse();
        sut.User.ShouldBeNull();
    }

    [Fact]
    public void IsAuthenticated_false_for_anonymous_principal()
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };
        var sut = new HttpContextCurrentUser(AccessorFor(context));

        sut.IsAuthenticated.ShouldBeFalse();
        sut.User.ShouldBeNull();
    }

    [Fact]
    public void Returns_projected_user_when_authenticated()
    {
        var sut = new HttpContextCurrentUser(AccessorFor(AuthenticatedContext()));

        sut.IsAuthenticated.ShouldBeTrue();
        sut.User.ShouldNotBeNull();
    }
}
