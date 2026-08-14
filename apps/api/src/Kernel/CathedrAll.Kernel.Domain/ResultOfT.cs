namespace CathedrAll.Kernel.Domain;

public sealed class Result<TValue> : Result
{
    internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error) => StoredValue = value;

    public TValue Value => IsSuccess
        ? StoredValue!
        : throw new InvalidOperationException(
            "Não é possível acessar o valor de um resultado malsucedido.");

    private TValue? StoredValue { get; }

    public static implicit operator Result<TValue>(TValue value) =>
        Success(value);

    public static implicit operator Result<TValue>(Error error) =>
        Failure<TValue>(error);
}
