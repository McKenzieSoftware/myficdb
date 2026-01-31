using System.Text.RegularExpressions;

namespace MyFicDB.Core.Helpers
{
    /// <summary>
    /// Helpers for Name cleaning/normalization in Services
    /// </summary>
    public static partial class NamePipeline
    {
        /// <summary>
        /// Receives a list of names, cleans up their casing, removes and dupes and limits to 30.
        /// </summary>
        public static List<string> CleanDedupeAndLimit(IEnumerable<string> rawNames, int max = 30)
        {
            return rawNames
                .Select(CleanDisplayName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(max)
                .ToList();
        }

        public static string NormalizeUpper(string displayName)
        {
            return (displayName ?? string.Empty).Trim().ToUpperInvariant();
        }

        /// <summary>
        /// Receives a list of names as csv, parses and removes any empty entries and trims them, then runs <see cref="CleanDedupeAndLimit(IEnumerable{string}, int)"/>
        /// against the clean raw values
        /// </summary>
        public static List<string> ParseCsvAndClean(string? csv, int max = 30)
        {
            if (string.IsNullOrWhiteSpace(csv))
            {
                return [];
            }

            var raw = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return CleanDedupeAndLimit(raw, max);
        }

        /// <summary>
        /// Receives a single name and cleans it up
        /// </summary>
        public static string CleanDisplayName(string input)
        {
            var cleaned = (input ?? string.Empty).Trim();
            cleaned = Regex.Replace(cleaned, @"\s+", " ");
            return cleaned;
        }

        /// <summary>
        /// Converts display name (Dark Comedy) to slug (dark-comedy)
        /// </summary>
        public static string Slugify(string input)
        {
            input = (input ?? string.Empty).Trim().ToLowerInvariant();
            input = Regex.Replace(input, @"\s+", "-");
            input = Regex.Replace(input, @"[^a-z0-9\-]", "");
            input = Regex.Replace(input, @"\-{2,}", "-").Trim('-');
            return input;
        }
    }
}
