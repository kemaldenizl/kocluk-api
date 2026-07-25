using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Security.Application.Common.Tenancy;
using Security.Domain.Authorization;
using Security.Domain.Memberships;
using Security.Domain.Tenancy;
using Security.Domain.Users;
using Security.Infrastructure.Security;
using Microsoft.Extensions.Hosting;

namespace Security.Infrastructure.Persistence.Seed;

public sealed class IdentitySeeder(
    SecurityDbContext dbContext,
    PasswordHasher passwordHasher,
    IOptions<IdentitySeedOptions> options,
    IOptions<TenantDefaultsOptions> tenantDefaults,
    IHostEnvironment hostEnvironment,
    ILogger<IdentitySeeder> logger
)
{
    private const string TenantAdminRoleName = "Admin";
    private const string TenantAdminRoleNormalized = "ADMIN";
    private const string MemberRoleName = "Member";
    private const string MemberRoleNormalized = "MEMBER";

    private readonly IdentitySeedOptions _options = options.Value;
    private readonly TenantDefaultsOptions _tenantDefaults = tenantDefaults.Value;
    private readonly IHostEnvironment _hostEnvironment = hostEnvironment;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Identity seed is disabled.");
            return;
        }

        if (_hostEnvironment.IsProduction() && !_options.AllowInProduction)
        {
            logger.LogWarning("Identity seed is blocked in Production environment.");
            return;
        }

        var publicTenantId = _tenantDefaults.PublicTenantId == Guid.Empty
            ? TenantDefaultsOptions.PublicTenantIdValue
            : _tenantDefaults.PublicTenantId;

        await SeedPublicTenantAsync(publicTenantId, cancellationToken);
        await SeedPermissionsAsync(cancellationToken);
        var systemAdminRole = await SeedPlatformSystemAdminRoleAsync(cancellationToken);
        var tenantAdminRole = await SeedTenantAdminRoleAsync(publicTenantId, cancellationToken);
        await SeedPublicMemberRoleAsync(publicTenantId, cancellationToken);
        await SeedAdminUserAsync(systemAdminRole, tenantAdminRole, publicTenantId, cancellationToken);
    }

    private async Task SeedPublicTenantAsync(Guid publicTenantId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Tenants.AnyAsync(x => x.Id == publicTenantId, cancellationToken);
        if (exists)
            return;

        var tenant = new Tenant(publicTenantId, "Public", "public", DateTime.UtcNow);

        await dbContext.Tenants.AddAsync(tenant, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        var permissionCodes = PermissionCatalog.All;

        var existingCodes = await dbContext.Permissions
            .AsNoTracking()
            .Select(x => x.Code)
            .ToListAsync(cancellationToken);

        var missingPermissions = permissionCodes
            .Except(existingCodes, StringComparer.OrdinalIgnoreCase)
            .Select(code => new Permission(Guid.NewGuid(), code))
            .ToList();

        if (missingPermissions.Count == 0)
            return;

        await dbContext.Permissions.AddRangeAsync(missingPermissions, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Platform-wide system administrator role, granted all permissions.</summary>
    private async Task<Role> SeedPlatformSystemAdminRoleAsync(CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles
            .Include(x => x.Permissions)
            .FirstOrDefaultAsync(
                x => x.Scope == RoleScope.Platform && x.NormalizedName == PlatformRoles.SystemAdminNormalized,
                cancellationToken);

        if (role is null)
        {
            role = Role.Platform(Guid.NewGuid(), PlatformRoles.SystemAdmin, PlatformRoles.SystemAdminNormalized);
            await dbContext.Roles.AddAsync(role, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await GrantAllPermissionsAsync(role, cancellationToken);
        return role;
    }

    /// <summary>Tenant-scoped admin role for the public tenant, granted all permissions.</summary>
    private async Task<Role> SeedTenantAdminRoleAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles
            .Include(x => x.Permissions)
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.NormalizedName == TenantAdminRoleNormalized,
                cancellationToken);

        if (role is null)
        {
            role = Role.ForTenant(Guid.NewGuid(), tenantId, TenantAdminRoleName, TenantAdminRoleNormalized);
            await dbContext.Roles.AddAsync(role, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await GrantAllPermissionsAsync(role, cancellationToken);
        return role;
    }

    /// <summary>Default role a self-registered user receives in the public tenant. No admin permissions.</summary>
    private async Task SeedPublicMemberRoleAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Roles.AnyAsync(
            x => x.TenantId == tenantId && x.NormalizedName == MemberRoleNormalized,
            cancellationToken);

        if (exists)
            return;

        var role = Role.ForTenant(Guid.NewGuid(), tenantId, MemberRoleName, MemberRoleNormalized);
        await dbContext.Roles.AddAsync(role, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task GrantAllPermissionsAsync(Role role, CancellationToken cancellationToken)
    {
        var allPermissionIds = await dbContext.Permissions
            .AsNoTracking()
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var permissionId in allPermissionIds)
        {
            role.AddPermission(permissionId, DateTime.UtcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAdminUserAsync(
        Role systemAdminRole,
        Role tenantAdminRole,
        Guid publicTenantId,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = _options.AdminEmail.Trim().ToUpperInvariant();

        var adminUser = await dbContext.Users
            .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);

        if (adminUser is null)
        {
            adminUser = new User(
                Guid.NewGuid(),
                _options.AdminEmail.Trim(),
                normalizedEmail,
                passwordHasher.Hash(_options.AdminPassword),
                "System",
                "Administrator",
                StudentType.Other,
                DateTime.UtcNow);

            adminUser.MarkEmailVerified();
            await dbContext.Users.AddAsync(adminUser, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // Grant the platform system-admin role (tenant-independent, all-tenant access).
        var hasPlatformRole = await dbContext.UserPlatformRoles
            .AnyAsync(x => x.UserId == adminUser.Id && x.RoleId == systemAdminRole.Id, cancellationToken);

        if (!hasPlatformRole)
        {
            await dbContext.UserPlatformRoles.AddAsync(
                new UserPlatformRole(adminUser.Id, systemAdminRole.Id, DateTime.UtcNow),
                cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // Also make the admin an active member of the public tenant with the tenant Admin role.
        var membership = await dbContext.Memberships
            .Include(x => x.Roles)
            .FirstOrDefaultAsync(x => x.UserId == adminUser.Id && x.TenantId == publicTenantId, cancellationToken);

        if (membership is null)
        {
            membership = new Membership(Guid.NewGuid(), adminUser.Id, publicTenantId, DateTime.UtcNow);
            await dbContext.Memberships.AddAsync(membership, cancellationToken);
        }

        membership.AddRole(tenantAdminRole.Id, DateTime.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
