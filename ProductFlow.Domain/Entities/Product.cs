
namespace ProductFlow.Domain.Entities;

public class Product
{
    public Guid Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public decimal Price { get; private set; }

    public int StockQuantity { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    // Entity Framework Core nesneyi oluşturabilsin diye.
    private Product()
    {
    }

    public Product(
        string code,
        string name,
        decimal price,
        int stockQuantity,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Ürün kodu boş olamaz.", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Ürün adı boş olamaz.", nameof(name));

        if (price < 0)
            throw new ArgumentOutOfRangeException(
                nameof(price), "Fiyat negatif olamaz.");

        if (stockQuantity < 0)
            throw new ArgumentOutOfRangeException(
                nameof(stockQuantity), "Stok miktarı negatif olamaz.");

        Id = Guid.NewGuid();
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description?.Trim();
        Price = price;
        StockQuantity = stockQuantity;
    }

    public void UpdateDetails(
        string name,
        string? description,
        decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Ürün adı boş olamaz.", nameof(name));

        if (price < 0)
            throw new ArgumentOutOfRangeException(
                nameof(price), "Fiyat negatif olamaz.");

        Name = name.Trim();
        Description = description?.Trim();
        Price = price;
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