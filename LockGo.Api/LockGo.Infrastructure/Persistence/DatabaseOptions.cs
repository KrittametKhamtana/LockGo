namespace LockGo.Infrastructure.Persistence;

/// <summary>Bound from the "Database" config section — structured fields instead of one raw connection string.</summary>
public class DatabaseOptions
{
    public const string SectionName = "Database";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 5432;
    public string Database { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int MaximumPoolSize { get; set; } = 10;

    /// <summary>Npgsql SSL mode name (e.g. "Require", "VerifyFull", "Disable"). Neon and most hosted Postgres need at least "Require".</summary>
    public string SslMode { get; set; } = "Require";
}
