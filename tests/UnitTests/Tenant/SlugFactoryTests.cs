using Tenant.Application.Common.Slugs;

namespace UnitTests.Tenant;

/// <summary>
/// Slugs are the stable, URL-safe tenant identifier, so the transformation must be deterministic and
/// forgiving of messy input. Each rule — lowercasing, accent stripping, hyphen collapsing, trimming — is
/// pinned down here.
/// </summary>
public sealed class SlugFactoryTests
{
    [Theory]
    [InlineData("Acme Inc.", "acme-inc")]
    [InlineData("  Hello World  ", "hello-world")]
    [InlineData("UPPER case", "upper-case")]
    [InlineData("multiple   spaces", "multiple-spaces")]
    [InlineData("a--b__c!!d", "a-b-c-d")]
    [InlineData("café crème", "cafe-creme")]
    [InlineData("already-a-slug", "already-a-slug")]
    [InlineData("trailing---", "trailing")]
    [InlineData("---leading", "leading")]
    [InlineData("2026 Cohort", "2026-cohort")]
    public void Create_produces_expected_slug(string input, string expected)
        => SlugFactory.Create(input).ShouldBe(expected);

    [Theory]
    [InlineData("")]
    [InlineData("!!!")]
    [InlineData("   ")]
    public void Create_returns_empty_when_no_alphanumerics(string input)
        => SlugFactory.Create(input).ShouldBe(string.Empty);

    [Fact]
    public void Create_throws_on_null()
        => Should.Throw<ArgumentNullException>(() => SlugFactory.Create(null!));

    [Fact]
    public void Create_never_yields_leading_or_trailing_hyphen()
    {
        var slug = SlugFactory.Create("  **weird**  ");

        slug.ShouldNotStartWith("-");
        slug.ShouldNotEndWith("-");
    }
}
