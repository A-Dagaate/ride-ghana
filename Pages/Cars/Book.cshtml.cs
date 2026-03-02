using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RideGhana.Data;
using RideGhana.Models;
using RideGhana.Services;

namespace RideGhana.Pages.Cars;

[Authorize]
public class BookModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly AvailabilityService _availability;

    public BookModel(ApplicationDbContext db, AvailabilityService availability)
    {
        _db = db;
        _availability = availability;
    }

    public Car Car { get; set; } = null!;

    [BindProperty] public DateTime StartDate { get; set; } = DateTime.Today;
    [BindProperty] public DateTime EndDate { get; set; } = DateTime.Today.AddDays(3);
    [BindProperty] public string PickupLocation { get; set; } = string.Empty;

    public int TotalDays => Math.Max(1, (int)(EndDate - StartDate).TotalDays);
    public decimal TotalCost => Car == null ? 0 : Car.DailyRate * TotalDays;

    public async Task<IActionResult> OnGetAsync(int id, DateTime? start, DateTime? end)
    {
        var car = await _db.Cars.FindAsync(id);
        if (car == null) return NotFound();

        Car = car;
        StartDate = start ?? DateTime.Today;
        EndDate = end ?? DateTime.Today.AddDays(3);
        PickupLocation = car.Location;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var car = await _db.Cars.FindAsync(id);
        if (car == null) return NotFound();
        Car = car;

        if (EndDate <= StartDate)
            ModelState.AddModelError("EndDate", "Return date must be after pick-up date.");

        if (!ModelState.IsValid) return Page();

        var available = await _availability.IsAvailableAsync(id, StartDate, EndDate);
        if (!available)
        {
            ModelState.AddModelError(string.Empty, "This car is no longer available for the selected dates.");
            return Page();
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        var totalCost = car.DailyRate * Math.Max(1, (int)(EndDate - StartDate).TotalDays);

        var reservation = new Reservation
        {
            UserId = userId,
            CarId = id,
            StartDate = StartDate,
            EndDate = EndDate,
            PickupLocation = PickupLocation,
            TotalCost = totalCost,
            Status = ReservationStatus.Pending
        };

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();

        return RedirectToPage("/Checkout/Index", new { reservationId = reservation.Id });
    }
}
