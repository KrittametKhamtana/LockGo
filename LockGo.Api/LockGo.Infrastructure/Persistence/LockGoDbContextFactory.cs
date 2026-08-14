using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LockGo.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef migrations add` build the model without a live DB connection
/// or the Api project's full DI/config setup. The connection string here is only
/// used to pick the Npgsql provider — no connection is actually opened.
/// </summary>
public class LockGoDbContextFactory : IDesignTimeDbContextFactory<LockGoDbContext>
{
    public LockGoDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LockGoDbContext>();
        optionsBuilder
            .UseNpgsql("Host=localhost;Database=lockgo;Username=lockgo;Password=lockgo")
            .UseSnakeCaseNamingConvention();

        return new LockGoDbContext(optionsBuilder.Options);
    }
}
