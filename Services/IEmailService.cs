using RideGhana.Models;

namespace RideGhana.Services;

public interface IEmailService
{
    Task SendReservationConfirmationAsync(Reservation reservation);
}
