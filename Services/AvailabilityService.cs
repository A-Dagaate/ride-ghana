using Microsoft.EntityFrameworkCore;
using RideGhana.Data;
using RideGhana.Models;

namespace RideGhana.Services;

public class AvailabilityService
{
    private readonly ApplicationDbContext _db;

    public AvailabilityService(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// A car is available if there are no Confirmed/Active reservations overlapping the window.
    /// Pending reservations do NOT block availability — cars stay available until payment completes.
    /// </summary>
    public async Task<bool> IsAvailableAsync(int carId, DateTime start, DateTime end)
    {
        var blocked = new[] { ReservationStatus.Confirmed, ReservationStatus.Active };

        return !await _db.Reservations
            .AnyAsync(r =>
                r.CarId == carId &&
                blocked.Contains(r.Status) &&
                r.StartDate < end &&
                r.EndDate > start);
    }

    public async Task<List<Car>> GetAvailableCarsAsync(DateTime start, DateTime end)
    {
        var blocked = new[] { ReservationStatus.Confirmed, ReservationStatus.Active };

        var bookedCarIds = await _db.Reservations
            .Where(r =>
                blocked.Contains(r.Status) &&
                r.StartDate < end &&
                r.EndDate > start)
            .Select(r => r.CarId)
            .ToListAsync();

        return await _db.Cars
            .Where(c => c.IsActive && !bookedCarIds.Contains(c.Id))
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();
    }
}
