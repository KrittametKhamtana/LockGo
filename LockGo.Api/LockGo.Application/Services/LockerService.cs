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

        var items = lockers.Select(locker =>
        {
            var distanceKm = hasOrigin
                ? HaversineDistanceKm(query.Lat!.Value, query.Lng!.Value, locker.Lat, locker.Lng)
                : (double?)null;

            return new LockerListItemDto(
                locker.Id,
                locker.Name,
                locker.Address,
                locker.Lat,
                locker.Lng,
                locker.OperatingStatus.ToString(),
                distanceKm,
                locker.Compartments.Count > 0 ? locker.Compartments.Min(c => c.Price) : 0m,
                locker.Compartments.Count(c => c.Status == CompartmentStatus.Available));
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

        return MapToDetailDto(locker);
    }

    private static LockerDetailDto MapToDetailDto(Locker locker) => new(
        locker.Id,
        locker.Name,
        locker.Address,
        locker.Lat,
        locker.Lng,
        locker.OperatingStatus.ToString(),
        locker.Compartments
            .Select(c => new CompartmentDto(c.Id, c.Size.ToString(), c.Price, c.Status.ToString()))
            .ToList());

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
