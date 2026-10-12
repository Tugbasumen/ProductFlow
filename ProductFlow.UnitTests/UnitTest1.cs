
using ProductFlow.Domain.Entities;

namespace ProductFlow.UnitTests;

public class ProductTests
{
    [Fact]
    public void Constructor_ValidData_CreatesProduct()
    {
        var product = new Product(
            "prd-001",
            "Laptop",
            25000m,
            10,
            "Ofis bilgisayarı");

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("PRD-001", product.Code);
        Assert.Equal("Laptop", product.Name);
        Assert.Equal(25000m, product.Price);
        Assert.Equal(10, product.StockQuantity);
        Assert.True(product.IsActive);
    }

    [Fact]
    public void Constructor_NegativePrice_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Product("PRD-002", "Mouse", -100m, 5));
    }

    [Fact]
    public void Constructor_NegativeStock_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Product("PRD-003", "Keyboard", 500m, -1));
    }

    [Fact]
    public void AdjustStock_ValidChange_UpdatesStock()
    {
        var product = new Product("PRD-004", "Monitor", 5000m, 10);

        product.AdjustStock(-3);

        Assert.Equal(7, product.StockQuantity);
    }

    [Fact]
    public void AdjustStock_ChangeWouldMakeStockNegative_ThrowsException()
    {
        var product = new Product("PRD-005", "Headset", 1500m, 2);

        Assert.Throws<InvalidOperationException>(() =>
            product.AdjustStock(-3));

        Assert.Equal(2, product.StockQuantity);
    }

    [Fact]
    public void UpdateDetails_ValidData_UpdatesProduct()
    {
        var product = new Product("PRD-006", "Old Name", 100m, 5);

        product.UpdateDetails("New Name", "Updated description", 150m);

        Assert.Equal("New Name", product.Name);
        Assert.Equal("Updated description", product.Description);
        Assert.Equal(150m, product.Price);
    }

    [Fact]
    public void Deactivate_SetsProductInactive()
    {
        var product = new Product("PRD-007", "Tablet", 8000m, 4);

        product.Deactivate();

        Assert.False(product.IsActive);
    }
}