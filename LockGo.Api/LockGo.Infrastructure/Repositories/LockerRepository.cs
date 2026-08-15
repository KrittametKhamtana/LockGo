using LockGo.Application.Common;
using LockGo.Application.DTOs;
using LockGo.Application.Interfaces;
using LockGo.Domain.Entities;
using LockGo.Domain.Enums;
using LockGo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LockGo.Infrastructure.Repositories;

public class LockerRepository : ILockerRepository
{
    private readonly LockGoDbContext _db;

    public LockerRepository(LockGoDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Locker>> SearchAsync(LockerSearchQuery query, CancellationToken ct)
    {
        var window = BookingWindow.From(query.StartTime, query.DurationHours);
        var windowStart = window.Start;
        var windowEnd = window.End;

        var lockers = _db.Lockers
            .AsNoTracking()
            .Include(l => l.Compartments)
                // Only reservations overlapping the requested window are loaded —
                // LockerService derives per-size availability from these rather
                // than from the denormalized Compartment.Status column, so the
                // list agrees with what the booking path will actually allow.
                .ThenInclude(c => c.Reservations.Where(r =>
                    r.Status == ReservationStatus.Active &&
                    r.StartTime < windowEnd &&
                    r.EndTime > windowStart))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // ToLower().Contains() rather than EF.Functions.ILike: ILike is
            // Npgsql-only and throws on the InMemory provider the integration
            // tests run against. This translates to LOWER(...) LIKE '%...%' on
            // Postgres — no index on that expression, but the locker table is
            // tiny and a functional index would be premature here.
            var term = query.Search.Trim().ToLower();
            lockers = lockers.Where(l =>
                l.Name.ToLower().Contains(term) ||
                l.Address.ToLower().Contains(term));
        }

        var hasSizeFilter = Enum.TryParse<CompartmentSize>(query.Size, ignoreCase: true, out var size);
        var availableOnly = query.AvailableOnly == true;

        // Size and availability must hold for the SAME compartment — two separate
        // Any() calls would pass a locker whose only free compartment is the wrong
        // size. Availability is evaluated against live reservations here too.
        if (hasSizeFilter && availableOnly)
        {
            lockers = lockers.Where(l => l.OperatingStatus == OperatingStatus.Open && l.Compartments.Any(c =>
                c.Size == size &&
                !c.Reservations.Any(r =>
                    r.Status == ReservationStatus.Active &&
                    r.StartTime < windowEnd &&
                    r.EndTime > windowStart)));
        }
        else if (hasSizeFilter)
        {
            lockers = lockers.Where(l => l.Compartments.Any(c => c.Size == size));
        }
        else if (availableOnly)
        {
            // A locker whose site is Closed can't actually be booked, even if
            // its compartments individually look free — "available only" must
            // exclude it, not just fully-booked lockers.
            lockers = lockers.Where(l => l.OperatingStatus == OperatingStatus.Open && l.Compartments.Any(c =>
                !c.Reservations.Any(r =>
                    r.Status == ReservationStatus.Active &&
                    r.StartTime < windowEnd &&
                    r.EndTime > windowStart)));
        }

        // Distance filtering can't be pushed down as SQL (Haversine over two runtime
        // coordinates), so it's applied in the service layer after this query returns.
        return await lockers.ToListAsync(ct);
    }

    public async Task<Locker?> GetByIdAsync(Guid id, BookingWindow window, CancellationToken ct)
    {
        var windowStart = window.Start;
        var windowEnd = window.End;

        return await _db.Lockers
            .AsNoTracking()
            .Include(l => l.Compartments)
                .ThenInclude(c => c.Reservations.Where(r =>
                    r.Status == ReservationStatus.Active &&
                    r.StartTime < windowEnd &&
                    r.EndTime > windowStart))
            .FirstOrDefaultAsync(l => l.Id == id, ct);
    }
}
