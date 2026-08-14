using LockGo.Application.Common.Exceptions;
using LockGo.Application.DTOs;
using LockGo.Application.Interfaces;
using LockGo.Domain.Entities;
using LockGo.Domain.Enums;

namespace LockGo.Application.Services;

public class LockerService : ILockerService
{
    private readonly ILockerRepository _lockerRepository;

    public LockerService(ILockerRepository lockerRepository)
    {
        _lockerRepository = lockerRepository;
    }

    public async Task<IReadOnlyList<LockerListItemDto>> SearchAsync(LockerSearchQuery query, CancellationToken ct)
    {
        var lockers = await _lockerRepository.SearchAsync(query, ct);

        var hasOrigin = query.Lat.HasValue && query.Lng.HasValue;
        var hasSizeFilter = Enum.TryParse<CompartmentSize>(query.Size, ignoreCase: true, out var sizeFilter);

        var items = lockers.Select(locker =>
        {
            var distanceKm = hasOrigin
                ? HaversineDistanceKm(query.Lat!.Value, query.Lng!.Value, locker.Lat, locker.Lng)
                : (double?)null;

            var sizeAvailability = BuildSizeAvailability(locker);

            // When a size filter is active, price/count headline figures describe
            // only that size — otherwise a "Small" search would show the locker's
            // Large price and count compartments the user didn't ask about.
            var relevant = hasSizeFilter
                ? sizeAvailability.Where(s => s.Size == sizeFilter.ToString()).ToList()
                : sizeAvailability;

            return new LockerListItemDto(
                locker.Id,
                locker.Name,
                locker.Address,
                locker.Lat,
                locker.Lng,
                locker.OperatingStatus.ToString(),
                distanceKm,
                relevant.Count > 0 ? relevant.Min(s => s.Price) : 0m,
                relevant.Sum(s => s.AvailableCount),
                sizeAvailability,
                sizeAvailability.Sum(s => s.AvailableCount) == 0);
        });

        if (hasOrigin && query.MaxDistanceKm.HasValue)
        {
            items = items.Where(i => i.DistanceKm is not null && i.DistanceKm <= query.MaxDistanceKm.Value);
        }

        return hasOrigin
            ? items.OrderBy(i => i.DistanceKm).ToList()
            : items.ToList();
    }

    public async Task<LockerDetailDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var locker = await _lockerRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("LOCKER_NOT_FOUND", $"Locker '{id}' was not found.");

        var sizeAvailability = BuildSizeAvailability(locker);

        return new LockerDetailDto(
            locker.Id,
            locker.Name,
            locker.Address,
            locker.Lat,
            locker.Lng,
            locker.OperatingStatus.ToString(),
            sizeAvailability,
            sizeAvailability.Sum(s => s.AvailableCount) == 0);
    }

    /// <summary>
    /// Groups a locker's compartments by size. Availability is derived from live
    /// Reservation rows (loaded by the repository) rather than the denormalized
    /// Compartment.Status column, so an expired-but-not-yet-swept reservation
    /// correctly frees its compartment — matching the write path's definition of
    /// "available" instead of drifting from it.
    /// Sizes with no compartments at all are omitted, which is what lets the UI
    /// show that a location simply doesn't offer Large.
    /// </summary>
    private static List<CompartmentSizeAvailabilityDto> BuildSizeAvailability(Locker locker)
    {
        var now = DateTimeOffset.UtcNow;

        return locker.Compartments
            .GroupBy(c => c.Size)
            .OrderBy(g => g.Key)
            .Select(group => new CompartmentSizeAvailabilityDto(
                group.Key.ToString(),
                group.Min(c => c.Price),
                group.Count(c => !HasActiveReservation(c, now)),
                group.Count()))
            .ToList();
    }

    private static bool HasActiveReservation(Compartment compartment, DateTimeOffset now) =>
        compartment.Reservations.Any(r => r.Status == ReservationStatus.Active && r.EndTime > now);

    /// <summary>Great-circle distance in km — real math, but the input coordinates are manually supplied (no device GPS/geocoding in this scope).</summary>
    private static double HaversineDistanceKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadiusKm = 6371.0;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLng = DegreesToRadians(lng2 - lng1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}
