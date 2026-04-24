namespace RefaccionariaCuate.Application.Common;

public sealed record Result<T>(bool Success, T? Data, string? Error = null)
{
    public static Result<T> Ok(T data) => new(true, data, null);
    public static Result<T> Fail(string error) => new(false, default, error);
}
