namespace Shared.Contracts.Commands;

public record RefundPayment
{
    public Guid OrderId { get; init; }
    public Guid PaymentId { get; init; }
    public decimal Amount { get; init; }
}