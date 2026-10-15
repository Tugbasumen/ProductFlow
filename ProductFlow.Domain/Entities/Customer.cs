namespace ProductFlow.Domain.Entities;

public sealed class Customer
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string? Phone { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    private Customer()
    {
    }

    public Customer(string name, string email, string? phone = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Müşteri adı boş olamaz.", nameof(name));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException(
                "E-posta boş olamaz.", nameof(email));

        Id = Guid.NewGuid();
        Name = name.Trim();
        Email = NormalizeEmail(email);
        Phone = NormalizePhone(phone);
    }

    public void UpdateDetails(string name, string email, string? phone)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Müşteri adı boş olamaz.", nameof(name));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException(
                "E-posta boş olamaz.", nameof(email));

        Name = name.Trim();
        Email = NormalizeEmail(email);
        Phone = NormalizePhone(phone);
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static string? NormalizePhone(string? phone)
    {
        return string.IsNullOrWhiteSpace(phone)
            ? null
            : phone.Trim();
    }
}
