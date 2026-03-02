namespace RideGhana.Models;

public enum ReservationStatus
{
    Pending,      // created, awaiting payment
    Confirmed,    // payment completed
    Active,       // car picked up
    Completed,    // car returned
    Cancelled
}

public class Reservation
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public int CarId { get; set; }
    public Car Car { get; set; } = null!;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string PickupLocation { get; set; } = "Accra, Ghana";
    public decimal TotalCost { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Payment? Payment { get; set; }

    public int TotalDays => Math.Max(1, (int)(EndDate - StartDate).TotalDays);
}
