using Microsoft.AspNetCore.Mvc.RazorPages;
using RideGhana.Data;
using RideGhana.Models;
using RideGhana.Services;

namespace RideGhana.Pages.Cars;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly AvailabilityService _availability;

    public IndexModel(ApplicationDbContext db, AvailabilityService availability)
    {
        _db = db;
        _availability = availability;
    }

    public List<Car> Cars { get; set; } = new();
    public DateTime Start { get; set; } = DateTime.Today;
    public DateTime End { get; set; } = DateTime.Today.AddDays(3);

    public async Task OnGetAsync(DateTime? start, DateTime? end)
    {
        Start = start ?? DateTime.Today;
        End = end ?? DateTime.Today.AddDays(3);

        if (End <= Start) End = Start.AddDays(1);

        Cars = await _availability.GetAvailableCarsAsync(Start, End);
    }
}
