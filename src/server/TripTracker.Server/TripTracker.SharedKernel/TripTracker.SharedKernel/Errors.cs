namespace TripTracker.SharedKernel.TripTracker.SharedKernel;

public class Errors
{
    public static class Validation
    {
        public static Error ValueTooLong(string name, int max)
        {
            return Error.Validation("value.too.long", $"{name} must not exceed {max} characters");
        }

        public static Error ValueInvalidLength<T>(string name, T min, T max)
            where T : struct, IComparable
        {
            return Error.Validation("value.invalid.length", $"{name} must be at least {min} - {max} characters long");
        }

        public static Error ValueTooSmall<T>(string name, T min)
            where T : struct, IComparable
        {
            return Error.Validation("value.too.small", $"{name} must be at least {min}");
        }

        public static Error ValueTooLarge<T>(string name, T max)
            where T : struct, IComparable
        {
            return Error.Validation("value.too.large", $"{name} must not exceed {max}");
        }

        public static Error ValueIsRequired(string? name = null)
        {
            var label = name ?? "value";
            return Error.Validation("value.is.required", $"{label} is required");
        }

        public static Error ValueIsInvalid(string name, string message)
        {
            return Error.Validation("value.is.invalid", $"{name} is invalid: {message}");
        }

        public static Error ArgumentNull(string name)
        {
            return Error.Validation("value.is.null", $"{name} is null");
        }
    }
}
