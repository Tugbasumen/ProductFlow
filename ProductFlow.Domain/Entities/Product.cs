
namespace ProductFlow.Domain.Entities;

public class Product
{
    public Guid Id { get; private set; }
    public Guid CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }

    public int MinimumStockLevel { get; private set; } = 5;

    public bool IsCriticalStock =>
        StockQuantity <= MinimumStockLevel;

    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    private Product()
    {
    }

    public Product(
        string code,
        string name,
        decimal price,
        int stockQuantity,
        Guid categoryId,
        string? description = null,
        int minimumStockLevel = 5)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException(
                "Ürün kodu boş olamaz.", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Ürün adı boş olamaz.", nameof(name));

        if (price < 0)
            throw new ArgumentOutOfRangeException(
                nameof(price), "Fiyat negatif olamaz.");

        if (stockQuantity < 0)
            throw new ArgumentOutOfRangeException(
                nameof(stockQuantity), "Stok miktarı negatif olamaz.");

        if (minimumStockLevel < 0)
            throw new ArgumentOutOfRangeException(
                nameof(minimumStockLevel),
                "Minimum stok seviyesi negatif olamaz.");

        Id = Guid.NewGuid();
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description?.Trim();
        Price = price;
        StockQuantity = stockQuantity;
        CategoryId = categoryId;
        MinimumStockLevel = minimumStockLevel;
    }

    public void UpdateDetails(
        string name,
        string? description,
        decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Ürün adı boş olamaz.", nameof(name));

        if (price < 0)
            throw new ArgumentOutOfRangeException(
                nameof(price), "Fiyat negatif olamaz.");

        Name = name.Trim();
        Description = description?.Trim();
        Price = price;
    }

    public void UpdateMinimumStockLevel(int minimumStockLevel)
    {
        if (minimumStockLevel < 0)
            throw new ArgumentOutOfRangeException(
                nameof(minimumStockLevel),
                "Minimum stok seviyesi negatif olamaz.");

        MinimumStockLevel = minimumStockLevel;
    }

    public void AdjustStock(int quantityChange)
    {
        int newQuantity = StockQuantity + quantityChange;

        if (newQuantity < 0)
            throw new InvalidOperationException(
                "Stok miktarı negatif olamaz.");

        StockQuantity = newQuantity;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
