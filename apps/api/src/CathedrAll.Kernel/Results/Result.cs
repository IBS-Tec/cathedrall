namespace CathedrAll.Kernel.Results;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Forbidden,
}

public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);
}

/// <summary>
/// Outcome of a request. Expected failures — a person who does not exist, a duplicate
/// document — travel as <see cref="Error"/>, not exceptions.
///
/// Exceptions stay for what is genuinely exceptional: the database is down, a bug. That
/// separation is what lets the log tell "the system broke" apart from "the user typed
/// something wrong", and only the first deserves waking someone up.
/// </summary>
public readonly record struct Result<T>
{
    private Result(T value)
    {
        Value = value;
        Error = null;
    }

    private Result(Error error)
    {
        Value = default;
        Error = error;
    }

    public T? Value { get; }
    public Error? Error { get; }

    public bool Success => Error is null;

    public static Result<T> Ok(T value) => new(value);
    public static Result<T> Fail(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Ok(value);
    public static implicit operator Result<T>(Error error) => Fail(error);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onError) =>
        Success ? onSuccess(Value!) : onError(Error!);
}
