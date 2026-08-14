using LockGo.Application.Interfaces;
using LockGo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LockGo.Tests.Integration;

/// <summary>
/// Boots the real Program pipeline (routing, DI, controllers, middleware) but
/// swaps Postgres for an EF Core InMemory database, scoped per factory
/// instance so tests in the same class share state but different classes
/// don't. InMemory doesn't support relational transactions, so IUnitOfWork is
/// also swapped for a pass-through that still calls SaveChanges — fine for
/// read-path and single-request write-path tests, but it can't reproduce the
/// Postgres-specific xmin/unique-constraint races (that's what
/// Concurrency/DoubleClickConfirmTests covers instead).
/// </summary>
public class LockGoWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"LockGoTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<LockGoDbContext>>();
            services.AddDbContext<LockGoDbContext>(options => options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<IUnitOfWork>();
            services.AddScoped<IUnitOfWork, TestUnitOfWork>();
        });
    }

    public async Task SeedAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LockGoDbContext>();
        await db.Database.EnsureCreatedAsync();
        await DbSeeder.SeedAsync(db);
    }
}
