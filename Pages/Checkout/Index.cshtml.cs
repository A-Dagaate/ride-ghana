using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RideGhana.Data;
using RideGhana.Models;
using RideGhana.Services;
using Stripe;
using System.Security.Claims;

namespace RideGhana.Pages.Checkout;

// IgnoreAntiforgeryToken is applied here so the Stripe webhook POST (which
// comes from Stripe's servers, not a browser) can reach OnPostWebhookAsync.
// Security for OnPostVerifyPaystackAsync is enforced by authentication +
// matching the reference to the current user's payment record + Paystack
// server-side verification.
[Authorize]
[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;
    private readonly PaystackService _paystack;

    public IndexModel(
        ApplicationDbContext db,
        IEmailService emailService,
        IConfiguration config,
        PaystackService paystack)
    {
        _db = db;
        _emailService = emailService;
        _config = config;
        _paystack = paystack;
    }

    public Reservation Reservation { get; set; } = null!;

    // ── Shared ────────────────────────────────────────────────────────────────
    public string SelectedCurrency { get; set; } = "GHS";
    public decimal DisplayAmount { get; set; }
    public string CurrencySymbol { get; set; } = "GHS";
    public string UserEmail { get; set; } = string.Empty;

    // ── Stripe (USD) ──────────────────────────────────────────────────────────
    public string StripePublishableKey { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    // ── Paystack (GHS) ────────────────────────────────────────────────────────
    public string PaystackPublicKey { get; set; } = string.Empty;
    public string PaystackReference { get; set; } = string.Empty;

    // ── GET ───────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnGetAsync(int reservationId, string currency = "GHS")
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var reservation = await _db.Reservations
            .Include(r => r.Car)
            .Include(r => r.User)
            .Include(r => r.Payment)
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId);

        if (reservation == null) return NotFound();
        if (reservation.Status == ReservationStatus.Confirmed)
            return RedirectToPage("/Reservations/Index");

        Reservation = reservation;
        UserEmail = reservation.User.Email ?? string.Empty;
        SelectedCurrency = currency == "USD" ? "USD" : "GHS";

        var ghsToUsd = double.Parse(_config["ExchangeRate:GhsToUsd"] ?? "0.065");
        if (SelectedCurrency == "USD")
        {
            DisplayAmount = Math.Round(reservation.TotalCost * (decimal)ghsToUsd, 2);
            CurrencySymbol = "USD";
            await InitStripeAsync(reservation, DisplayAmount);
        }
        else
        {
            DisplayAmount = reservation.TotalCost;
            CurrencySymbol = "GHS";
            InitPaystack(reservation);
        }

        return Page();
    }

    // ── Stripe: create PaymentIntent ──────────────────────────────────────────
    private async Task InitStripeAsync(Reservation reservation, decimal usdAmount)
    {
        StripePublishableKey = _config["Stripe:PublishableKey"]!;

        if (reservation.Payment == null)
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(usdAmount * 100),
                Currency = "usd",
                Metadata = new Dictionary<string, string>
                {
                    { "reservation_id", reservation.Id.ToString() },
                    { "charged_currency", "USD" }
                }
            };
            var intent = await new PaymentIntentService().CreateAsync(options);

            var payment = new Payment
            {
                ReservationId = reservation.Id,
                Amount = usdAmount,
                Currency = "USD",
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
    }

    // ── Paystack: generate / reuse reference ──────────────────────────────────
    private void InitPaystack(Reservation reservation)
    {
        PaystackPublicKey = _config["Paystack:PublicKey"]!;

        if (reservation.Payment == null)
        {
            // Reference is stored to DB in OnGetAsync caller after this returns.
            // We generate it here so the view can render it immediately.
            PaystackReference = $"RG-{reservation.Id}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

            var payment = new Payment
            {
                ReservationId = reservation.Id,
                Amount = reservation.TotalCost,
                Currency = "GHS",
                PaystackReference = PaystackReference
            };
            _db.Payments.Add(payment);
            // Saved synchronously via fire-and-forget is risky; save inline.
            _db.SaveChanges();
        }
        else
        {
            PaystackReference = reservation.Payment.PaystackReference ?? string.Empty;
        }
    }

    // ── POST: Paystack verify (called by JS after popup success) ──────────────
    public async Task<IActionResult> OnPostVerifyPaystackAsync([FromBody] PaystackVerifyRequest req)
    {
        if (string.IsNullOrWhiteSpace(req?.Reference))
            return new JsonResult(new { ok = false, message = "Invalid reference." });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var payment = await _db.Payments
            .Include(p => p.Reservation).ThenInclude(r => r.Car)
            .Include(p => p.Reservation).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(p =>
                p.PaystackReference == req.Reference &&
                p.Reservation.UserId == userId);

        if (payment == null)
            return new JsonResult(new { ok = false, message = "Payment record not found." });

        if (payment.Status == PaymentStatus.Succeeded)
        {
            // Already confirmed (e.g. double-tap) — just redirect.
            return new JsonResult(new
            {
                ok = true,
                redirect = Url.Page("/Reservations/Index", new { success = true })
            });
        }

        var verified = await _paystack.VerifyTransactionAsync(req.Reference);
        if (!verified)
            return new JsonResult(new { ok = false, message = "Payment could not be verified. Please contact support." });

        payment.Status = PaymentStatus.Succeeded;
        payment.PaidAt = DateTime.UtcNow;
        payment.Reservation.Status = ReservationStatus.Confirmed;
        await _db.SaveChangesAsync();

        await _emailService.SendReservationConfirmationAsync(payment.Reservation);

        return new JsonResult(new
        {
            ok = true,
            redirect = Url.Page("/Reservations/Index", new { success = true })
        });
    }

    // ── POST: Stripe webhook (called by Stripe servers) ───────────────────────
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

    public record PaystackVerifyRequest(string Reference);
}
