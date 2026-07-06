namespace OrderService.Domain;

public class Order
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalAmount => Quantity * UnitPrice;
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    // EF Core needs a parameterless constructor; keep it private so nobody
    // outside this class can materialize an invalid Order by accident.
    private Order() { }

    public static Order Create(Guid customerId, Guid productId, int quantity, decimal unitPrice)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive.", nameof(quantity));
        if (unitPrice <= 0) throw new ArgumentException("Unit price must be positive.", nameof(unitPrice));

        return new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Status = OrderStatus.Placed,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}