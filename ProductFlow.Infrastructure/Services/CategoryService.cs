
using Microsoft.EntityFrameworkCore;
using ProductFlow.Application.DTOs;
using ProductFlow.Application.Interfaces;
using ProductFlow.Domain.Entities;
using ProductFlow.Infrastructure.Persistence;

namespace ProductFlow.Infrastructure.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly ProductFlowDbContext _dbContext;

    public CategoryService(ProductFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Category?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .FirstOrDefaultAsync(
                category => category.Id == id,
                cancellationToken);
    }

    public async Task<Category> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = request.Name.Trim();

        var nameExists = await _dbContext.Categories
            .AnyAsync(
                category => category.Name == normalizedName,
                cancellationToken);

        if (nameExists)
            throw new InvalidOperationException(
                "Bu kategori adı zaten kayıtlı.");

        var category = new Category(
            request.Name,
            request.Description);

        _dbContext.Categories.Add(category);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return category;
    }

    public async Task<bool> UpdateAsync(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(
                category => category.Id == id,
                cancellationToken);

        if (category is null)
            return false;

        var normalizedName = request.Name.Trim();

        var nameExists = await _dbContext.Categories
            .AnyAsync(
                existing => existing.Id != id &&
                            existing.Name == normalizedName,
                cancellationToken);

        if (nameExists)
            throw new InvalidOperationException(
                "Bu kategori adı zaten kayıtlı.");

        category.UpdateDetails(
            request.Name,
            request.Description);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(
                category => category.Id == id,
                cancellationToken);

        if (category is null)
            return false;

        category.Deactivate();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
