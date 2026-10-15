using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProductFlow.Application.DTOs;
using ProductFlow.Application.Exceptions;
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

    public async Task<PagedResult<Product>> GetAllAsync(
        Guid? categoryId = null,
        string? search = null,
        int page = 1,
        int pageSize = 10,
        string sortBy = "name",
        string sortOrder = "asc",
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(page),
                "Sayfa numarası en az 1 olmalıdır.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                "Sayfa boyutu 1 ile 100 arasında olmalıdır.");
        }

        sortBy = (sortBy ?? "name").Trim().ToLowerInvariant();
        sortOrder = (sortOrder ?? "asc").Trim().ToLowerInvariant();

        if (sortBy is not ("name" or "price" or "stockquantity"))
        {
            throw new ArgumentException(
                "Sıralama alanı name, price veya stockQuantity olmalıdır.",
                nameof(sortBy));
        }

        if (sortOrder is not ("asc" or "desc"))
        {
            throw new ArgumentException(
                "Sıralama yönü asc veya desc olmalıdır.",
                nameof(sortOrder));
        }

        IQueryable<Product> query = _dbContext.Products
            .AsNoTracking();

        if (categoryId.HasValue)
        {
            query = query.Where(
                product => product.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(product =>
                product.Name.Contains(search) ||
                product.Code.Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var isSqlite =
            _dbContext.Database.ProviderName ==
            "Microsoft.EntityFrameworkCore.Sqlite";

        List<Product> items;

        if (isSqlite && sortBy == "price")
        {
            // SQLite, decimal türündeki ORDER BY işlemini desteklemez.
            // Bu dal test verileri için fiyat sıralamasını bellekte yapar.
            var products = await query
                .Include(product => product.Category)
                .ToListAsync(cancellationToken);

            IOrderedEnumerable<Product> orderedProducts =
                sortOrder == "asc"
                    ? products.OrderBy(product => product.Price)
                    : products.OrderByDescending(product => product.Price);

            items = orderedProducts
                .ThenBy(product => product.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }
        else
        {
            IOrderedQueryable<Product> orderedQuery = sortBy switch
            {
                "price" when sortOrder == "asc" =>
                    query.OrderBy(product => product.Price),

                "price" =>
                    query.OrderByDescending(product => product.Price),

                "stockquantity" when sortOrder == "asc" =>
                    query.OrderBy(product => product.StockQuantity),

                "stockquantity" =>
                    query.OrderByDescending(product => product.StockQuantity),

                _ when sortOrder == "asc" =>
                    query.OrderBy(product => product.Name),

                _ =>
                    query.OrderByDescending(product => product.Name)
            };

            items = await orderedQuery
                .ThenBy(product => product.Id)
                .Include(product => product.Category)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        return new PagedResult<Product>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<Product?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .Include(product => product.Category)
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
        {
            return false;
        }

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
        {
            return false;
        }

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
        {
            return false;
        }

        product.Deactivate();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}