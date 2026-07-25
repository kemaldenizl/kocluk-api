using BuildingBlocks.Authorization;

namespace UnitTests.BuildingBlocks;

/// <summary>
/// The catalog is the single source of truth wired into both the identity seeder and policy registration.
/// If it drifts from <see cref="PermissionCodes"/> (duplicate, missing, or stray entry) services silently
/// disagree about what a permission means, so these invariants are asserted explicitly.
/// </summary>
public sealed class PermissionCatalogTests
{
    [Fact]
    public void All_entries_are_unique()
        => PermissionCatalog.All.ShouldBeUnique();

    [Fact]
    public void Contains_exactly_the_declared_permission_codes()
    {
        var declared = typeof(PermissionCodes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .ToArray();

        PermissionCatalog.All.ShouldBe(declared, ignoreOrder: true);
    }

    [Fact]
    public void All_codes_follow_the_area_dot_action_convention()
    {
        foreach (var code in PermissionCatalog.All)
            code.ShouldMatch("^[a-z]+\\.[a-z]+$");
    }
}
