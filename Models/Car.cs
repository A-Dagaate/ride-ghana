namespace RideGhana.Models;

public class Car
{
    public int Id { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Category { get; set; } = string.Empty; // Economy, SUV, Luxury, etc.
    public decimal DailyRate { get; set; }
    public string ImagePath { get; set; } = "/images/cars/placeholder.jpg";
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = "Accra, Ghana";
    public int Seats { get; set; }
    public string Transmission { get; set; } = "Automatic";
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
