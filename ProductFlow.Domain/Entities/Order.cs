namespace ProductFlow.Domain.Entities;

public enum OrderStatus
{
    Pending = 1,
    Confirmed = 2,
    Cancelled = 3
}

public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public Customer Customer { get; private set; } = null!;

    public OrderStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    public DateTime? ConfirmedAtUtc { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items;

    public decimal TotalAmount => _items.Sum(item => item.LineTotal);

    private Order()
    {
    }

    public Order(Guid customerId)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException(
                "Müşteri kimliği boş olamaz.", nameof(customerId));

        Id = Guid.NewGuid();
        CustomerId = customerId;
        Status = OrderStatus.Pending;
    }

    public void AddItem(Guid productId, int quantity, decimal unitPrice)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException(
                "Yalnızca bekleyen siparişe kalem eklenebilir.");

        _items.Add(new OrderItem(Id, productId, quantity, unitPrice));
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException(
                "Yalnızca bekleyen sipariş onaylanabilir.");

        if (_items.Count == 0)
            throw new InvalidOperationException(
                "Boş sipariş onaylanamaz.");

        Status = OrderStatus.Confirmed;
        ConfirmedAtUtc = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Cancelled)
            throw new InvalidOperationException(
                "Sipariş zaten iptal edilmiş.");

        Status = OrderStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
    }
}
