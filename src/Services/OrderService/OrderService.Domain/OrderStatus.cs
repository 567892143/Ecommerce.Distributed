namespace OrderService.Domain;

public enum OrderStatus
{
    Placed,
    PaymentPending,
    PaymentConfirmed,
    PaymentFailed,
    InventoryReserved,
    InventoryFailed,
    Shipped,
    Cancelled
}