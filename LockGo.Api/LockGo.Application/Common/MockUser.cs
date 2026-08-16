namespace LockGo.Application.Common;

/// <summary>Single hardcoded user shared by seed data and reservation creation — no real auth in this scope.</summary>
public static class MockUser
{
    public const int Id = 1;
    public const string Name = "Jane Doe";
}
