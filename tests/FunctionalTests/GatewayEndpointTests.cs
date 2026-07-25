using System.Net;
using System.Net.Http.Headers;

namespace FunctionalTests;

/// <summary>
/// Black-box tests of the gateway's edge behavior: health/identity endpoints, and the authentication
/// boundary that must reject anonymous callers on protected routes while letting the public auth routes
/// through. Downstream clusters are intentionally unreachable, so "passed edge auth" is asserted as
/// "not 401" rather than a 2xx (which would require a live service).
/// </summary>
public sealed class GatewayEndpointTests(GatewayApplicationFactory factory) : IClassFixture<GatewayApplicationFactory>
{
    private readonly GatewayApplicationFactory _factory = factory;

    [Fact]
    public async Task Health_live_returns_ok()
    {
        var response = await _factory.CreateClient().GetAsync("/health/live");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Root_returns_gateway_identity()
    {
        var response = await _factory.CreateClient().GetAsync("/");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldContain("ApiGateway");
    }

    [Fact]
    public async Task Protected_route_without_token_is_unauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/users/me");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Protected_route_with_garbage_token_is_unauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");

        var response = await client.GetAsync("/api/users/me");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Protected_route_with_expired_token_is_unauthorized()
    {
        var client = _factory.CreateClient();
        var expired = _factory.CreateAccessToken(expiresAtUtc: DateTime.UtcNow.AddMinutes(-5));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expired);

        var response = await client.GetAsync("/api/users/me");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Protected_route_with_valid_token_passes_edge_auth()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateAccessToken());

        var response = await client.GetAsync("/api/users/me");

        // Edge auth accepted the token; the request is forwarded to the (unreachable) Security cluster,
        // so anything other than 401 proves the gateway did not reject the caller itself.
        response.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Public_auth_route_is_reachable_without_token()
    {
        var response = await _factory.CreateClient().PostAsync("/api/auth/login", content: null);

        // The "anonymous" route policy lets it past edge auth; it then fails forwarding downstream.
        response.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized);
    }
}
