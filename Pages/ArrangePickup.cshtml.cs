using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RideGhana.Models;
using System.ComponentModel.DataAnnotations;

namespace RideGhana.Pages;

[Authorize]
public class ArrangePickupModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ArrangePickupModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public const string DefaultPickupAddress = "Accra Road, Ejisu Junction, Ashanti Region, Ghana";

    // Ejisu Junction coordinates (used for client-side distance calc)
    public const double EjisuLat = 6.7166;
    public const double EjisuLon = -1.4730;

    [BindProperty] public bool PreferDelivery { get; set; }

    [BindProperty]
    [Display(Name = "Delivery Address")]
    public string DeliveryAddress { get; set; } = string.Empty;

    public async Task<IActionResult> OnPostAsync()
    {
        if (PreferDelivery && string.IsNullOrWhiteSpace(DeliveryAddress))
            ModelState.AddModelError(nameof(DeliveryAddress), "Please enter a delivery address.");

        if (!ModelState.IsValid) return Page();

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        user.PreferDelivery = PreferDelivery;
        user.DeliveryAddress = PreferDelivery ? DeliveryAddress.Trim() : string.Empty;
        await _userManager.UpdateAsync(user);

        TempData["Success"] = PreferDelivery
            ? $"Delivery arranged to: {user.DeliveryAddress}"
            : "Pick-up confirmed at Accra Road, Ejisu Junction.";

        return RedirectToPage("/Cars/Index");
    }
}
