using System.Text.Json.Nodes;

namespace RideGhana.Services;

public class PaystackService
{
    private readonly HttpClient _http;
    private readonly ILogger<PaystackService> _logger;

    public PaystackService(HttpClient http, ILogger<PaystackService> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>
    /// Calls the Paystack verify endpoint and returns true only when
    /// the transaction data.status == "success".
    /// </summary>
    public async Task<bool> VerifyTransactionAsync(string reference)
    {
        try
        {
            var response = await _http.GetAsync(
                $"https://api.paystack.co/transaction/verify/{Uri.EscapeDataString(reference)}");

            var body = await response.Content.ReadAsStringAsync();
            var json = JsonNode.Parse(body);
            return json?["data"]?["status"]?.GetValue<string>() == "success";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Paystack verification failed for reference {Reference}", reference);
            return false;
        }
    }
}
