using System.Reflection;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Security.Application.Abstractions.Tenancy;
using Security.Domain.Abstractions;
using Security.Domain.Authorization;
using Security.Domain.Auditing;
using Security.Domain.Sessions;
using Security.Domain.Tenancy;
using Security.Domain.Tokens;
using Security.Domain.Users;
using Security.Domain.Mfa;

namespace Security.Infrastructure.Persistence;

public sealed class SecurityDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<RefreshSession> RefreshSessions => Set<RefreshSession>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<PasswordChangeToken> PasswordChangeTokens => Set<PasswordChangeToken>();
    public DbSet<EmailChangeToken> EmailChangeTokens => Set<EmailChangeToken>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<MfaMethod> MfaMethods => Set<MfaMethod>();
    public DbSet<RecoveryCode> RecoveryCodes => Set<RecoveryCode>();

    public SecurityDbContext(DbContextOptions<SecurityDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("security");

        builder.ApplyConfigurationsFromAssembly(typeof(SecurityDbContext).Assembly);

        builder.AddInboxStateEntity();
        builder.AddOutboxMessageEntity();
        builder.AddOutboxStateEntity();

        builder.UseOpenIddict();

        ApplyTenantQueryFilters(builder);
    }

    /// <summary>
    /// Applies a per-tenant global query filter to every <see cref="ITenantScoped"/> entity so reads
    /// are isolated automatically. The filter references the injected <see cref="ITenantContext"/>, which
    /// EF Core evaluates as a parameter on every query — no repository has to add a tenant predicate.
    /// </summary>
    private void ApplyTenantQueryFilters(ModelBuilder builder)
    {
        var tenantScopedTypes = builder.Model.GetEntityTypes()
            .Where(entityType => typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
            .ToList();

        foreach (var entityType in tenantScopedTypes)
        {
            typeof(SecurityDbContext)
                .GetMethod(nameof(SetTenantQueryFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(entityType.ClrType)
                .Invoke(this, [builder]);
        }
    }

    private void SetTenantQueryFilter<TEntity>(ModelBuilder builder)
        where TEntity : class, ITenantScoped
    {
        builder.Entity<TEntity>().HasQueryFilter(entity => entity.TenantId == _tenantContext.TenantId);
    }
}
