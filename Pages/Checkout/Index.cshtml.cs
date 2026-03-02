using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RideGhana.Data;
using RideGhana.Models;
using RideGhana.Services;
using Stripe;

namespace RideGhana.Pages.Checkout;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;

    public IndexModel(ApplicationDbContext db, IEmailService emailService, IConfiguration config)
    {
        _db = db;
        _emailService = emailService;
        _config = config;
    }

    public Reservation Reservation { get; set; } = null!;
    public string StripePublishableKey { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    // Currency selected on the Book page ("GHS" or "USD")
    public string SelectedCurrency { get; set; } = "GHS";
    public decimal DisplayAmount { get; set; }
    public string CurrencySymbol { get; set; } = "GHS";

    public async Task<IActionResult> OnGetAsync(int reservationId, string currency = "GHS")
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;

        var reservation = await _db.Reservations
            .Include(r => r.Car)
            .Include(r => r.User)
            .Include(r => r.Payment)
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId);

        if (reservation == null) return NotFound();
        if (reservation.Status == ReservationStatus.Confirmed)
            return RedirectToPage("/Reservations/Index");

        Reservation = reservation;
        StripePublishableKey = _config["Stripe:PublishableKey"]!;
        SelectedCurrency = currency == "USD" ? "USD" : "GHS";

        // Compute display amount and Stripe charge amount
        var ghsToUsd = double.Parse(_config["ExchangeRate:GhsToUsd"] ?? "0.065");
        if (SelectedCurrency == "USD")
        {
            DisplayAmount = Math.Round(reservation.TotalCost * (decimal)ghsToUsd, 2);
            CurrencySymbol = "USD";
        }
        else
        {
            DisplayAmount = reservation.TotalCost;
            CurrencySymbol = "GHS";
        }

        // Create PaymentIntent if not yet created for this reservation
        if (reservation.Payment == null)
        {
            // Stripe amount is always in the smallest currency unit (cents / pesewas)
            long stripeAmount;
            string stripeCurrency;
            if (SelectedCurrency == "USD")
            {
                stripeAmount = (long)(DisplayAmount * 100);
                stripeCurrency = "usd";
            }
            else
            {
                stripeAmount = (long)(reservation.TotalCost * 100);
                stripeCurrency = "ghs";
            }

            var options = new PaymentIntentCreateOptions
            {
                Amount = stripeAmount,
                Currency = stripeCurrency,
                Metadata = new Dictionary<string, string>
                {
                    { "reservation_id", reservation.Id.ToString() },
                    { "charged_currency", SelectedCurrency }
                }
            };
            var service = new PaymentIntentService();
            var intent = await service.CreateAsync(options);

            var payment = new Payment
            {
                ReservationId = reservation.Id,
                Amount = DisplayAmount,
                Currency = SelectedCurrency,
                StripePaymentIntentId = intent.Id,
                StripeClientSecret = intent.ClientSecret
            };
            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();
            ClientSecret = intent.ClientSecret;
        }
        else
        {
            ClientSecret = reservation.Payment.StripeClientSecret ?? string.Empty;
        }

        return Page();
    }

    // Webhook endpoint — called by Stripe on payment success
    public async Task<IActionResult> OnPostWebhookAsync()
    {
        var json = await new StreamReader(Request.Body).ReadToEndAsync();
        var webhookSecret = _config["Stripe:WebhookSecret"]!;

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json, Request.Headers["Stripe-Signature"], webhookSecret);

            if (stripeEvent.Type == "payment_intent.succeeded")
            {
                var intent = stripeEvent.Data.Object as PaymentIntent;
                if (intent?.Metadata.TryGetValue("reservation_id", out var resIdStr) == true
                    && int.TryParse(resIdStr, out var resId))
                {
                    var reservation = await _db.Reservations
                        .Include(r => r.Car)
                        .Include(r => r.User)
                        .Include(r => r.Payment)
                        .FirstOrDefaultAsync(r => r.Id == resId);

                    if (reservation != null && reservation.Status == ReservationStatus.Pending)
                    {
                        reservation.Status = ReservationStatus.Confirmed;
                        if (reservation.Payment != null)
                        {
                            reservation.Payment.Status = PaymentStatus.Succeeded;
                            reservation.Payment.PaidAt = DateTime.UtcNow;
                        }
                        await _db.SaveChangesAsync();
                        await _emailService.SendReservationConfirmationAsync(reservation);
                    }
                }
            }
        }
        catch (StripeException)
        {
            return BadRequest();
        }

        return new OkResult();
    }
}
