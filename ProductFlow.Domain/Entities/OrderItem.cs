namespace ProductFlow.Domain.Entities;

public sealed class OrderItem
{
    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public Order Order { get; private set; } = null!;

    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => Quantity * UnitPrice;

    private OrderItem()
    {
    }

    public OrderItem(
        Guid orderId,
        Guid productId,
        int quantity,
        decimal unitPrice)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException(
                "Sipariş kimliği boş olamaz.", nameof(orderId));

        if (productId == Guid.Empty)
            throw new ArgumentException(
                "Ürün kimliği boş olamaz.", nameof(productId));

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Sipariş kalemi miktarı sıfırdan büyük olmalıdır.");

        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice),
                "Birim fiyat negatif olamaz.");

        Id = Guid.NewGuid();
        OrderId = orderId;
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
