namespace ProductFlow.Application.Exceptions;

public sealed class DuplicateProductCodeException : Exception
{
    public DuplicateProductCodeException()
    : base("Bu ürün kodu zaten kayıtlı.")
    {
    }
}
