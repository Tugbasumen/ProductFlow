using ProductFlow.Application.DTOs;
using ProductFlow.Domain.Entities;

namespace ProductFlow.Application.Interfaces;

public interface ICustomerService
{
    Task<IReadOnlyList<Customer>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Customer?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Customer> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        Guid id,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
