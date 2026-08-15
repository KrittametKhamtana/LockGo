namespace LockGo.Infrastructure.Security;

/// <summary>Bound from the "Jwt" config section — same pattern as DatabaseOptions.</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Symmetric signing key. This default lets the app (and tests) start
    /// without any local config — override it with a real random value in
    /// your gitignored appsettings.Development.json for anything beyond
    /// local dev, the same way Database credentials are handled.
    /// </summary>
    public string Secret { get; set; } = "insecure-default-dev-jwt-signing-key-change-me-32-bytes-minimum";

    public string Issuer { get; set; } = "LockGo";
    public string Audience { get; set; } = "LockGo";
    public int ExpiryMinutes { get; set; } = 60 * 24 * 7;
}
