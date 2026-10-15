using System;
using System.Threading;
using System.Threading.Tasks;
using ProductFlow.Application.DTOs;
using ProductFlow.Domain.Entities;

namespace ProductFlow.Application.Interfaces;

public interface IProductService
{
    Task<PagedResult<Product>> GetAllAsync(
    Guid? categoryId = null,
    string? search = null,
    int page = 1,
    int pageSize = 10,
    string sortBy = "name",
    string sortOrder = "asc",
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