namespace LockGo.Domain.Entities;

/// <summary>
/// <see cref="Name"/> is the original mock-user field, still used by the single
/// hardcoded row the booking flow writes reservations against (see MockUser) —
/// unrelated to and untouched by real accounts.
///
/// The fields below are for real, registered accounts. They're nullable
/// because the mock-user row never populates them; a row is either "the mock
/// user" (Name only) or "a real account" (everything below set).
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Username { get; set; }
    public string? PasswordHash { get; set; }

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
