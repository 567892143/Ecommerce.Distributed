namespace InventoryService.Domain;

public class ProductStock
{
    public Guid ProductId { get; private set; }
    public int AvailableQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }

    private ProductStock() { }

    public static ProductStock Create(Guid productId, int initialQuantity) => new()
    {
        ProductId = productId,
        AvailableQuantity = initialQuantity,
        ReservedQuantity = 0
    };

    public bool TryReserve(int quantity)
    {
        if (AvailableQuantity < quantity) return false;

        AvailableQuantity -= quantity;
        ReservedQuantity += quantity;
        return true;
    }

    // Not used until Phase 10 — the compensating action when we need to
    // undo a reservation because a later step in the saga failed.
    public void Release(int quantity)
    {
        ReservedQuantity -= quantity;
        AvailableQuantity += quantity;
    }
}