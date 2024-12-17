using System;

namespace ForecastingModule.Helper
{
    public class Optional<T>
    {
        private readonly T value;
        public bool HasValue { get; }

        private Optional(T value, bool hasValue)
        {
            this.value = value;
            HasValue = hasValue;
        }

        public static Optional<T> Of(T value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Value cannot be null.");
            }
            return new Optional<T>(value, true);
        }

        public static Optional<T> Empty()
        {
            return new Optional<T>(default, false);
        }

        public T GetValueOrDefault(T defaultValue = default)
        {
            return HasValue ? value : defaultValue;
        }

        public T Get()
        {
            if (!HasValue)
            {
                throw new InvalidOperationException("No value present.");
            }
            return value;
        }

        public override string ToString()
        {
            return HasValue ? $"Optional[{value}]" : "Optional.Empty";
        }
    }

}
