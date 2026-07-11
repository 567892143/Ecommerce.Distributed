namespace PaymentService.Api.Infrastructure;

public class PaymentGatewayClient
{
    private readonly HttpClient _httpClient;

    public PaymentGatewayClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<bool> ChargeAsync(Guid orderId, decimal amount, CancellationToken ct)
    {
        var response = await _httpClient.PostAsJsonAsync("/charge", new { OrderId = orderId, Amount = amount }, ct);
        return response.IsSuccessStatusCode;
    }
}