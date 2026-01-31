using System.ComponentModel.DataAnnotations;

namespace MyFicDB.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CsvListAttribute : ValidationAttribute
    {
        public int MaxItems { get; init; }
        public int MaxTokenLength { get; init;}
        public int MaxRawLength { get; init; }

        public CsvListAttribute()
        {
            ErrorMessage = "Invalid list form.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var s = value as string;

            if (string.IsNullOrWhiteSpace(s))
            {
                return ValidationResult.Success;
            }

            if (s.Length > MaxRawLength)
            {
                return new ValidationResult($"Value is too long (max {MaxRawLength} characters).");
            }

            var parts = s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // Dedup case-insensitively to match pipeline behavior
            var distinct = parts
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (distinct.Count > MaxItems)
            {
                return new ValidationResult($"Too many items (max {MaxItems}).");
            }

            foreach (var token in distinct)
            {
                if (token.Length > MaxTokenLength)
                {
                    return new ValidationResult($"An item is too long (max {MaxTokenLength} characters): \"{token[..Math.Min(token.Length, 20)]}...\"");
                }
            }

            return ValidationResult.Success;
        }
    }
}
