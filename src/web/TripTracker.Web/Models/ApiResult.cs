namespace TripTracker.Web.Models;

/// <summary>Outcome of an API call: either a value or at least one error.</summary>
public sealed class ApiResult<T>
    where T : class
{
    private readonly T? _value;

    private ApiResult(T? value, IReadOnlyList<ApiError> errors)
    {
        _value = value;
        Errors = errors;
    }

    /// <summary>Whether the call returned a value.</summary>
    public bool IsSuccess => _value is not null;

    /// <summary>The returned value.</summary>
    /// <exception cref="InvalidOperationException">The call failed.</exception>
    public T Value => _value ?? throw new InvalidOperationException("A failed result has no value.");

    /// <summary>Why the call failed; empty on success.</summary>
    public IReadOnlyList<ApiError> Errors { get; }

    /// <summary>A successful result.</summary>
    public static ApiResult<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new ApiResult<T>(value, []);
    }

    /// <summary>A failed result.</summary>
    /// <exception cref="ArgumentException">No error was given.</exception>
    public static ApiResult<T> Failure(IReadOnlyList<ApiError> errors)
    {
        if (errors.Count == 0)
        {
            throw new ArgumentException("A failed result needs at least one error.", nameof(errors));
        }

        return new ApiResult<T>(null, errors);
    }
}
