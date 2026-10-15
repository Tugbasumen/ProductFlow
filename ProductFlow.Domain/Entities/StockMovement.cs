namespace ProductFlow.Domain.Entities;

public enum StockMovementType
{
    In = 1,
    Out = 2
}

public sealed class StockMovement
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public StockMovementType Type { get; private set; }

    public int Quantity { get; private set; }

    public string? Description { get; private set; }

    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    private StockMovement()
    {
    }

    public StockMovement(
        Guid productId,
        StockMovementType type,
        int quantity,
        string? description = null)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException(
                "Ürün kimliği boş olamaz.", nameof(productId));

        if (type is not StockMovementType.In
            and not StockMovementType.Out)
            throw new ArgumentOutOfRangeException(nameof(type));

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(quantity), "Miktar sıfırdan büyük olmalıdır.");

        if (description?.Length > 500)
            throw new ArgumentException(
                "Açıklama en fazla 500 karakter olabilir.",
                nameof(description));

        ProductId = productId;
        Type = type;
        Quantity = quantity;
        Description = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
    }
}