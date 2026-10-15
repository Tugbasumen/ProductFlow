using ProductFlow.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using ProductFlow.Application.DTOs;
using ProductFlow.Application.Interfaces;
using ProductFlow.Domain.Entities;
using ProductFlow.Infrastructure.Persistence;

namespace ProductFlow.Infrastructure.Services;

public sealed class ProductService : IProductService
{
    private readonly ProductFlowDbContext _dbContext;

    public ProductService(ProductFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Product?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .FirstOrDefaultAsync(
                product => product.Id == id,
                cancellationToken);
    }


    public async Task<Product> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var codeExists = await _dbContext.Products
            .AnyAsync(
                product => product.Code == normalizedCode,
                cancellationToken);

        if (codeExists)
        {
            throw new DuplicateProductCodeException();
        }

        var categoryExists = await _dbContext.Categories
            .AnyAsync(
                category => category.Id == request.CategoryId,
                cancellationToken);

        if (!categoryExists)
        {
            throw new InvalidOperationException(
                "Belirtilen kategori bulunamadı.");
        }

        var product = new Product(
            request.Code,
            request.Name,
            request.Price,
            request.StockQuantity,
            request.CategoryId,
            request.Description);

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return product;
    }


    public async Task<bool> UpdateAsync(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await GetByIdAsync(id, cancellationToken);

        if (product is null)
            return false;

        product.UpdateDetails(
            request.Name,
            request.Description,
            request.Price);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> AdjustStockAsync(
        Guid id,
        int quantityChange,
        CancellationToken cancellationToken = default)
    {
        var product = await GetByIdAsync(id, cancellationToken);

        if (product is null)
            return false;

        product.AdjustStock(quantityChange);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var product = await GetByIdAsync(id, cancellationToken);

        if (product is null)
            return false;

        product.Deactivate();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}