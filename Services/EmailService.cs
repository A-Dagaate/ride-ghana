using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using RideGhana.Models;

namespace RideGhana.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendReservationConfirmationAsync(Reservation reservation)
    {
        var notificationAddress = _config["Email:NotificationAddress"]!;
        var customerEmail = reservation.User?.Email ?? "unknown";

        var subject = $"Booking Confirmed - Reservation #{reservation.Id} | Ride Ghana";
        var body = BuildEmailBody(reservation);

        // Send to customer
        await SendAsync(customerEmail, subject, body);

        // Send notification copy to admin
        await SendAsync(notificationAddress, $"[New Booking] {subject}", body);
    }

    private string BuildEmailBody(Reservation r)
    {
        return $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family:sans-serif;color:#333;max-width:600px;margin:auto;padding:20px">
              <div style="background:#1a7a4a;padding:20px;border-radius:8px 8px 0 0">
                <h1 style="color:white;margin:0">🚗 Ride Ghana</h1>
              </div>
              <div style="border:1px solid #ddd;border-top:none;padding:24px;border-radius:0 0 8px 8px">
                <h2>Booking Confirmed!</h2>
                <p>Dear {r.User?.FullName ?? "Valued Customer"},</p>
                <p>Your reservation has been confirmed. Here are your booking details:</p>
                <table style="width:100%;border-collapse:collapse;margin:16px 0">
                  <tr style="background:#f5f5f5">
                    <td style="padding:10px;font-weight:bold">Reservation #</td>
                    <td style="padding:10px">{r.Id}</td>
                  </tr>
                  <tr>
                    <td style="padding:10px;font-weight:bold">Vehicle</td>
                    <td style="padding:10px">{r.Car?.Year} {r.Car?.Make} {r.Car?.Model}</td>
                  </tr>
                  <tr style="background:#f5f5f5">
                    <td style="padding:10px;font-weight:bold">Pick-up</td>
                    <td style="padding:10px">{r.StartDate:dddd, MMMM d yyyy}</td>
                  </tr>
                  <tr>
                    <td style="padding:10px;font-weight:bold">Return</td>
                    <td style="padding:10px">{r.EndDate:dddd, MMMM d yyyy}</td>
                  </tr>
                  <tr style="background:#f5f5f5">
                    <td style="padding:10px;font-weight:bold">Duration</td>
                    <td style="padding:10px">{r.TotalDays} day(s)</td>
                  </tr>
                  <tr>
                    <td style="padding:10px;font-weight:bold">Location</td>
                    <td style="padding:10px">{r.PickupLocation}</td>
                  </tr>
                  <tr style="background:#1a7a4a;color:white">
                    <td style="padding:10px;font-weight:bold">Total Paid</td>
                    <td style="padding:10px;font-weight:bold">GHS {r.TotalCost:N2}</td>
                  </tr>
                </table>
                <p>Please present this confirmation and a valid driver's licence at pick-up.</p>
                <p style="color:#888;font-size:12px">Ride Ghana · Accra, Ghana · support@rideghana.com</p>
              </div>
            </body>
            </html>
            """;
    }

    private async Task SendAsync(string to, string subject, string htmlBody)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                _config["Email:FromName"] ?? "Ride Ghana",
                _config["Email:FromAddress"] ?? "no-reply@rideghana.com"));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(
                _config["Email:SmtpHost"],
                int.Parse(_config["Email:SmtpPort"]!),
                SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(
                _config["Email:SmtpUser"], _config["Email:SmtpPass"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
        }
    }
}
