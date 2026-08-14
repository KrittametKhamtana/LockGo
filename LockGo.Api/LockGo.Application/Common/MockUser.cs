namespace LockGo.Application.Common;

/// <summary>Single hardcoded user shared by seed data and reservation creation — no real auth in this scope.</summary>
public static class MockUser
{
    public static readonly Guid Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public const string Name = "Jane Doe";
}
