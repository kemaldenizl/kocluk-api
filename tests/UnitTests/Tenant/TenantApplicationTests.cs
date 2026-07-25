using FluentValidation.TestHelper;
using Tenant.Application.Tenants;
using Tenant.Application.Tenants.CreateTenant;
using Tenant.Application.Tenants.RenameTenant;

namespace UnitTests.Tenant;

public sealed class CreateTenantValidatorTests
{
    private readonly CreateTenantCommandValidator _validator = new();

    [Fact]
    public void Valid_with_slug_passes()
        => _validator.TestValidate(new CreateTenantCommand("Acme Inc.", "acme-inc")).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Valid_without_slug_passes()
        => _validator.TestValidate(new CreateTenantCommand("Acme Inc.", null)).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Rejects_empty_name()
        => _validator.TestValidate(new CreateTenantCommand("", null)).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Rejects_name_over_200_chars()
        => _validator.TestValidate(new CreateTenantCommand(new string('a', 201), null)).ShouldHaveValidationErrorFor(x => x.Name);

    [Theory]
    [InlineData("Has Spaces")]
    [InlineData("UPPER")]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    [InlineData("double--hyphen")]
    [InlineData("under_score")]
    public void Rejects_malformed_slug(string slug)
        => _validator.TestValidate(new CreateTenantCommand("Acme", slug)).ShouldHaveValidationErrorFor(x => x.Slug);

    [Fact]
    public void Rejects_slug_over_100_chars()
        => _validator.TestValidate(new CreateTenantCommand("Acme", new string('a', 101))).ShouldHaveValidationErrorFor(x => x.Slug);
}

public sealed class RenameTenantValidatorTests
{
    private readonly RenameTenantCommandValidator _validator = new();

    [Fact]
    public void Valid_passes()
        => _validator.TestValidate(new RenameTenantCommand(Guid.NewGuid(), "New Name")).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Rejects_empty_tenant_and_name()
    {
        var result = _validator.TestValidate(new RenameTenantCommand(Guid.Empty, ""));
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}

public sealed class TenantMapperTests
{
    [Fact]
    public void ToDto_projects_every_field()
    {
        var tenant = new TenantBuilder()
            .WithName("Acme Inc.")
            .WithSlug("acme-inc")
            .WithCreatedAt(TestClock.UtcNow)
            .Build();

        var dto = tenant.ToDto();

        dto.Id.ShouldBe(tenant.Id);
        dto.Name.ShouldBe("Acme Inc.");
        dto.Slug.ShouldBe("acme-inc");
        dto.IsActive.ShouldBeTrue();
        dto.CreatedAtUtc.ShouldBe(TestClock.UtcNow);
        dto.UpdatedAtUtc.ShouldBe(tenant.UpdatedAtUtc);
    }

    [Fact]
    public void ToDto_reflects_deactivation()
    {
        var tenant = TenantBuilder.Default();
        tenant.Deactivate(TestClock.OneHourLater);

        tenant.ToDto().IsActive.ShouldBeFalse();
    }
}
