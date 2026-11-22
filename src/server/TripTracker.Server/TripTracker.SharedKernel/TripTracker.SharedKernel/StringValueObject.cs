using CSharpFunctionalExtensions;

namespace TripTracker.SharedKernel.TripTracker.SharedKernel;

public abstract class StringValueObject : ComparableValueObject
{
    public string Value { get; }

    protected StringValueObject(string value)
    {
        Value = value;
    }

    public override string ToString() => Value;

    protected static Result<T, Error> Create<T>(
        string value,
        int minLength,
        int maxLength,
        Func<string, T> factory)
        where T : StringValueObject
    {
        if (string.IsNullOrWhiteSpace(value))
            return Errors.Validation.ValueIsRequired(typeof(T).Name);

        value = value.Trim();

        if (value.Length < minLength)
            return Errors.Validation.ValueTooSmall(typeof(T).Name, minLength);

        if (value.Length > maxLength)
            return Errors.Validation.ValueTooLong(typeof(T).Name, maxLength);

        return factory(value);
    }

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return Value;
    }
}
