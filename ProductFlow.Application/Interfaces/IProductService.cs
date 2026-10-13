
using ProductFlow.Application.DTOs;
using ProductFlow.Domain.Entities;

namespace ProductFlow.Application.Interfaces;

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Product> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> AdjustStockAsync(
        Guid id,
        int quantityChange,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}