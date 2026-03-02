using Microsoft.AspNetCore.Identity;

namespace RideGhana.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? DriversLicenseNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Pickup / delivery preference (set on the ArrangePickup onboarding page)
    public bool PreferDelivery { get; set; } = false;
    public string DeliveryAddress { get; set; } = string.Empty;

    // Drive option chosen at registration: "Self" or "Driver"
    public string DriveOption { get; set; } = "Self";

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
