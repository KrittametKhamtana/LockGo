namespace LockGo.Domain.Entities;

/// <summary>
/// Mock user — no real auth in this scope. A single hardcoded row is seeded.
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
