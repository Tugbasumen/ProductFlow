using Microsoft.EntityFrameworkCore;
using ProductFlow.Application.DTOs;
using ProductFlow.Application.Interfaces;
using ProductFlow.Domain.Entities;
using ProductFlow.Infrastructure.Persistence;

namespace ProductFlow.Infrastructure.Services;

public sealed class CustomerService : ICustomerService
{
    private readonly ProductFlowDbContext _dbContext;

    public CustomerService(ProductFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .AsNoTracking()
            .OrderBy(customer => customer.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Customer?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .FirstOrDefaultAsync(
                customer => customer.Id == id,
                cancellationToken);
    }

    public async Task<Customer> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var emailExists = await _dbContext.Customers
            .AnyAsync(
                customer => customer.Email == normalizedEmail,
                cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException(
                "Bu e-posta adresi zaten kayıtlı.");
        }

        var customer = new Customer(
            request.Name,
            request.Email,
            request.Phone);

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return customer;
    }

    public async Task<bool> UpdateAsync(
        Guid id,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(
                existing => existing.Id == id,
                cancellationToken);

        if (customer is null)
            return false;

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var emailExists = await _dbContext.Customers
            .AnyAsync(
                existing => existing.Id != id &&
                            existing.Email == normalizedEmail,
                cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException(
                "Bu e-posta adresi zaten kayıtlı.");
        }

        customer.UpdateDetails(
            request.Name,
            request.Email,
            request.Phone);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(
                existing => existing.Id == id,
                cancellationToken);

        if (customer is null)
            return false;

        customer.Deactivate();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
