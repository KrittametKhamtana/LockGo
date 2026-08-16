using LockGo.Domain.Enums;

namespace LockGo.Domain.Entities;

public class Locker
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lng { get; set; }
    public OperatingStatus OperatingStatus { get; set; } = OperatingStatus.Open;

    public ICollection<Compartment> Compartments { get; set; } = new List<Compartment>();
}
