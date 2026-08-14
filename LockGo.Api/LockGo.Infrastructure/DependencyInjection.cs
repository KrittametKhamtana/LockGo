using LockGo.Application.Interfaces;
using LockGo.Infrastructure.Persistence;
using LockGo.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LockGo.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Full production wiring: Postgres-backed DbContext + repositories.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        services.AddDbContext<LockGoDbContext>(options =>
            options
                .UseNpgsql(connectionString, npgsql =>
                {
                    // Free-tier server: small pool, low retry count to fail fast
                    // instead of piling up connections under load.
                    npgsql.MaxBatchSize(20);
                    npgsql.EnableRetryOnFailure(maxRetryCount: 2);
                })
                .UseSnakeCaseNamingConvention());

        return services.AddRepositories();
    }

    /// <summary>
    /// Repositories + IUnitOfWork only, no DbContext registration — lets tests
    /// register their own (InMemory) DbContext without EF Core seeing two
    /// competing provider configurations for the same context type.
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<ILockerRepository, LockerRepository>();
        services.AddScoped<ICompartmentRepository, CompartmentRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        return services;
    }
}
