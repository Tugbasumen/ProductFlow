
using Microsoft.EntityFrameworkCore;
using ProductFlow.Application.DTOs;
using ProductFlow.Domain.Entities;
using ProductFlow.Infrastructure.Persistence;
using ProductFlow.Infrastructure.Services;

namespace ProductFlow.UnitTests;

public class CategoryServiceTests
{
    private static ProductFlowDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ProductFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ProductFlowDbContext(options);
    }

    [Fact]
    public async Task GetAllAsync_WhenCategoriesExist_ReturnsCategories()
    {
        using var dbContext = CreateDbContext();
        var service = new CategoryService(dbContext);

        dbContext.Categories.Add(new Category("Elektronik"));
        dbContext.Categories.Add(new Category("Giyim"));
        await dbContext.SaveChangesAsync();

        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Elektronik", result[0].Name);
        Assert.Equal("Giyim", result[1].Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCategoryDoesNotExist_ReturnsNull()
    {
        using var dbContext = CreateDbContext();
        var service = new CategoryService(dbContext);

        var result = await service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesCategory()
    {
        using var dbContext = CreateDbContext();
        var service = new CategoryService(dbContext);

        var request = new CreateCategoryRequest
        {
            Name = "Elektronik",
            Description = "Elektronik ürünler"
        };

        var result = await service.CreateAsync(request);

        Assert.Equal("Elektronik", result.Name);
        Assert.Equal("Elektronik ürünler", result.Description);
        Assert.Single(await dbContext.Categories.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_ThrowsException()
    {
        using var dbContext = CreateDbContext();
        var service = new CategoryService(dbContext);

        dbContext.Categories.Add(new Category("Elektronik"));
        await dbContext.SaveChangesAsync();

        var request = new CreateCategoryRequest
        {
            Name = "Elektronik"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(request));
    }

    [Fact]
    public async Task UpdateAsync_WhenCategoryDoesNotExist_ReturnsFalse()
    {
        using var dbContext = CreateDbContext();
        var service = new CategoryService(dbContext);

        var request = new UpdateCategoryRequest
        {
            Name = "Yeni kategori"
        };

        var result = await service.UpdateAsync(Guid.NewGuid(), request);

        Assert.False(result);
    }

    [Fact]
    public async Task UpdateAsync_WhenCategoryExists_UpdatesDetails()
    {
        using var dbContext = CreateDbContext();
        var service = new CategoryService(dbContext);

        var category = new Category("Eski kategori", "Eski açıklama");
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();

        var request = new UpdateCategoryRequest
        {
            Name = "Yeni kategori",
            Description = "Yeni açıklama"
        };

        var result = await service.UpdateAsync(category.Id, request);

        Assert.True(result);
        Assert.Equal("Yeni kategori", category.Name);
        Assert.Equal("Yeni açıklama", category.Description);
    }

    [Fact]
    public async Task DeactivateAsync_WhenCategoryExists_DeactivatesCategory()
    {
        using var dbContext = CreateDbContext();
        var service = new CategoryService(dbContext);

        var category = new Category("Elektronik");
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();

        var result = await service.DeactivateAsync(category.Id);

        Assert.True(result);
        Assert.False(category.IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_WhenCategoryDoesNotExist_ReturnsFalse()
    {
        using var dbContext = CreateDbContext();
        var service = new CategoryService(dbContext);

        var result = await service.DeactivateAsync(Guid.NewGuid());

        Assert.False(result);
    }
}
