namespace PaymentService.Domain;

public class Payment
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? RefundedAtUtc { get; private set; }

    private Payment() { }

    public static Payment CreatePending(Guid orderId, decimal amount) => new()
    {
        Id = Guid.NewGuid(),
        OrderId = orderId,
        Amount = amount,
        Status = PaymentStatus.Pending,
        CreatedAtUtc = DateTime.UtcNow
    };

    public void MarkSucceeded() => Status = PaymentStatus.Succeeded;

    public void MarkFailed(string reason)
    {
        Status = PaymentStatus.Failed;
        FailureReason = reason;
    }

      public void MarkRefunded()
    {
        if (Status != PaymentStatus.Succeeded)
            throw new InvalidOperationException($"Cannot refund a payment in status {Status}.");

        Status = PaymentStatus.Refunded;
        RefundedAtUtc = DateTime.UtcNow;
    }
}