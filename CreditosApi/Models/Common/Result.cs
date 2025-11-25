namespace CreditosApi.Models.Common;
public class Result<T>
{

    public T Value { get; }
    public string Error { get; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    private Result(T value)
    {
        Value = value;
        Error = string.Empty;
        IsSuccess = true;
    }

    private Result(string error)
    {
        Value = default!;
        Error = error ?? "Erro desconhecido";
        IsSuccess = false;
    }

    public static Result<T> Success(T value) => new Result<T>(value);
    public static Result<T> Failure(string error) => new Result<T>(error);

    // Métodos úteis para functional programming
    public Result<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        return IsSuccess
            ? Result<TResult>.Success(mapper(Value))
            : Result<TResult>.Failure(Error);
    }

    public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> binder)
    {
        return IsSuccess
            ? binder(Value)
            : Result<TResult>.Failure(Error);
    }

    public T ValueOr(T defaultValue) => IsSuccess ? Value : defaultValue;
}

public class Result
{
    public string Error { get; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    private Result(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        Error = error ?? string.Empty;
    }

    public static Result Success() => new Result(true, string.Empty);
    public static Result Failure(string error) => new Result(false, error);

    public static Result Combine(params Result[] results)
    {
        foreach (var result in results)
        {
            if (result.IsFailure)
                return Failure(result.Error);
        }
        return Success();
    }
}
