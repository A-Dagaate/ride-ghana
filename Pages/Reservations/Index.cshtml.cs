using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RideGhana.Data;
using RideGhana.Models;

namespace RideGhana.Pages.Reservations;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db) => _db = db;

    public List<Reservation> Reservations { get; set; } = new();

    public async Task OnGetAsync(bool success = false)
    {
        if (success)
            TempData["Success"] = "Payment confirmed! Your booking is now active. A confirmation email has been sent.";

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;

        Reservations = await _db.Reservations
            .Include(r => r.Car)
            .Include(r => r.Payment)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;

        var reservation = await _db.Reservations
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (reservation == null) return NotFound();

        if (reservation.Status == ReservationStatus.Pending)
        {
            reservation.Status = ReservationStatus.Cancelled;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Reservation #{id} cancelled.";
        }
        else
        {
            TempData["Error"] = "Only pending reservations can be cancelled.";
        }

        return RedirectToPage();
    }
}
