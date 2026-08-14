using LockGo.Application.Interfaces;
using LockGo.Domain.Entities;
using LockGo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LockGo.Infrastructure.Repositories;

public class CompartmentRepository : ICompartmentRepository
{
    private readonly LockGoDbContext _db;

    public CompartmentRepository(LockGoDbContext db)
    {
        _db = db;
    }

    public async Task<Compartment?> GetByIdWithLockerAsync(Guid id, CancellationToken ct)
    {
        // Tracked (no AsNoTracking): the reservation write path mutates Status on
        // this same instance and relies on SaveChanges picking up the change.
        return await _db.Compartments
            .Include(c => c.Locker)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }
}
