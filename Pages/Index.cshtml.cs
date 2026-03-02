using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RideGhana.Data;
using RideGhana.Models;

namespace RideGhana.Pages;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public List<Car> FeaturedCars { get; set; } = new();

    public async Task OnGetAsync()
    {
        FeaturedCars = await _db.Cars
            .Where(c => c.IsActive)
            .Take(5)
            .ToListAsync();
    }
}
