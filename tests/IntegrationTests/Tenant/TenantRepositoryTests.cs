using Microsoft.EntityFrameworkCore;
using Tenant.Infrastructure.Persistence;
using Tenant.Infrastructure.Persistence.Repositories;
using DomainTenant = Tenant.Domain.Tenants.Tenant;

namespace IntegrationTests.Tenant;

/// <summary>
/// Persistence-level tests that run the real <see cref="TenantDbContext"/> (entity configuration, backing
/// fields for private setters, repository query logic, unit-of-work commit) against an isolated in-memory
/// EF provider. Each test gets its own database, so they are order-independent and need no external server.
/// </summary>
public sealed class TenantRepositoryTests : IDisposable
{
    private readonly TenantDbContext _dbContext;
    private readonly TenantRepository _repository;
    private readonly UnitOfWork _unitOfWork;

    public TenantRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase($"tenant-tests-{Guid.NewGuid()}")
            .Options;

        _dbContext = new TenantDbContext(options);
        _repository = new TenantRepository(_dbContext);
        _unitOfWork = new UnitOfWork(_dbContext);
    }

    public void Dispose() => _dbContext.Dispose();

    private async Task<DomainTenant> Persist(DomainTenant tenant)
    {
        await _repository.AddAsync(tenant);
        await _unitOfWork.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear(); // force a fresh read from the store
        return tenant;
    }

    [Fact]
    public async Task Add_then_GetById_round_trips_all_fields()
    {
        var tenant = new TenantBuilder().WithName("Acme Inc.").WithSlug("acme-inc").Build();
        await Persist(tenant);

        var loaded = await _repository.GetByIdAsync(tenant.Id);

        loaded.ShouldNotBeNull();
        loaded.Name.ShouldBe("Acme Inc.");
        loaded.Slug.ShouldBe("acme-inc");
        loaded.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task GetById_returns_null_when_absent()
        => (await _repository.GetByIdAsync(Guid.NewGuid())).ShouldBeNull();

    [Fact]
    public async Task GetAll_returns_every_tenant()
    {
        await _repository.AddAsync(new TenantBuilder().WithSlug("a").Build());
        await _repository.AddAsync(new TenantBuilder().WithSlug("b").Build());
        await _unitOfWork.SaveChangesAsync();

        (await _repository.GetAllAsync()).Count.ShouldBe(2);
    }

    [Fact]
    public async Task ExistsBySlug_is_true_for_stored_slug()
    {
        await Persist(new TenantBuilder().WithSlug("acme-inc").Build());

        (await _repository.ExistsBySlugAsync("acme-inc")).ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsBySlug_normalizes_input_casing_and_whitespace()
    {
        await Persist(new TenantBuilder().WithSlug("acme-inc").Build());

        (await _repository.ExistsBySlugAsync("  ACME-INC  ")).ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsBySlug_is_false_when_absent()
        => (await _repository.ExistsBySlugAsync("missing")).ShouldBeFalse();

    [Fact]
    public async Task Mutations_are_persisted_on_commit()
    {
        var tenant = await Persist(new TenantBuilder().WithName("Old").Build());

        var tracked = await _repository.GetByIdAsync(tenant.Id);
        tracked!.Rename("New", TestClock.OneHourLater);
        tracked.Deactivate(TestClock.OneHourLater);
        await _unitOfWork.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var reloaded = await _repository.GetByIdAsync(tenant.Id);
        reloaded!.Name.ShouldBe("New");
        reloaded.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Remove_deletes_on_commit()
    {
        var tenant = await Persist(new TenantBuilder().Build());

        var tracked = await _repository.GetByIdAsync(tenant.Id);
        _repository.Remove(tracked!);
        await _unitOfWork.SaveChangesAsync();

        (await _repository.GetByIdAsync(tenant.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task Changes_are_not_saved_without_commit()
    {
        var tenant = new TenantBuilder().Build();
        await _repository.AddAsync(tenant);
        // Deliberately no SaveChangesAsync.
        _dbContext.ChangeTracker.Clear();

        (await _repository.GetByIdAsync(tenant.Id)).ShouldBeNull();
    }
}
