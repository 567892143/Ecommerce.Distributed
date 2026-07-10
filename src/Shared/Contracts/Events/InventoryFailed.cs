namespace Shared.Contracts.Events;

public record InventoryFailed
{
    public Guid OrderId { get; init; }
    public Guid ProductId { get; init; }
    public string Reason { get; init; } = default!;
    public DateTime OccurredAtUtc { get; init; }
}