namespace Shared.Contracts.Events;

public record InventoryReserved
{
    public Guid OrderId { get; init; }
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
    public DateTime OccurredAtUtc { get; init; }
}