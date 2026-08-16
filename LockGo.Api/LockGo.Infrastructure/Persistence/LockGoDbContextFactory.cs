using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace LockGo.Infrastructure.Persistence;

/// <summary>
/// Builds a DbContext for the `dotnet ef` tools, which can't go through the Api
/// project's DI container.
///
/// It reads the same Database section the running app does, because
/// `dotnet ef database update` opens a real connection — an earlier version
/// hardcoded a localhost connection string on the assumption that only
/// `migrations add` (which just builds the model offline) would ever use this,
/// and applying a migration then failed against a database that didn't exist.
///
/// Config comes from the Api project's appsettings files. ASPNETCORE_ENVIRONMENT
/// is honoured but defaults to Development, since that's the only environment
/// these tools are ever run in locally.
/// </summary>
public class LockGoDbContextFactory : IDesignTimeDbContextFactory<LockGoDbContext>
{
    public LockGoDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        // The tools run with the startup project as the working directory, but
        // fall back to walking up from Infrastructure so a plain `dotnet ef`
        // from the solution folder still finds the files.
        var basePath = ResolveApiProjectPath();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .Build();

        var dbOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
            ?? throw new InvalidOperationException(
                $"Config section '{DatabaseOptions.SectionName}' was not found. Looked in {basePath} for " +
                $"appsettings.json and appsettings.{environment}.json.");

        if (string.IsNullOrWhiteSpace(dbOptions.Host) || string.IsNullOrWhiteSpace(dbOptions.Database))
        {
            throw new InvalidOperationException(
                $"'{DatabaseOptions.SectionName}:Host' and '{DatabaseOptions.SectionName}:Database' must be set " +
                $"in appsettings.{environment}.json (found in {basePath}).");
        }

        var connectionString = new NpgsqlConnectionStringBuilder
        {
            Host = dbOptions.Host,
            Port = dbOptions.Port,
            Database = dbOptions.Database,
            Username = dbOptions.Username,
            Password = dbOptions.Password,
            MaxPoolSize = dbOptions.MaximumPoolSize,
            SslMode = Enum.Parse<SslMode>(dbOptions.SslMode, ignoreCase: true),
        }.ConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<LockGoDbContext>();
        optionsBuilder
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention();

        return new LockGoDbContext(optionsBuilder.Options);
    }

    private static string ResolveApiProjectPath()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "LockGo.Api");
            if (File.Exists(Path.Combine(candidate, "appsettings.json")))
            {
                return candidate;
            }

            if (File.Exists(Path.Combine(current.FullName, "appsettings.json")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
