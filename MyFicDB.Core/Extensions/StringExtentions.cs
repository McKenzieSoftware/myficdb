using System.Net;
using System.Text.RegularExpressions;

namespace MyFicDB.Core.Extensions
{
    public static class StringExtentions
    {
        /// <summary>
        /// Truncates existing HTML content to the specificed size, while also removing html tags
        /// </summary>
        public static string TruncateWords(this string html, int maxWords)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }

            // Remove HTML tags
            var plainText = Regex.Replace(html, "<.*?>", string.Empty);

            // Decode HTML entities (&mdash;, &rsquo;, &nbsp;, etc.)
            plainText = WebUtility.HtmlDecode(plainText);

            // Normalize whitespace
            plainText = Regex.Replace(plainText, @"\s+", " ").Trim();

            // Split into words
            var words = plainText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (words.Length <= maxWords)
            {
                return plainText;
            }

            return string.Join(' ', words.Take(maxWords)) + "…";
        }

        // Source - https://stackoverflow.com/a/62698159
        // Posted by zackmark15, modified by community. See post 'Timeline' for change history
        // Retrieved 2026-01-06, License - CC BY-SA 4.0
        public static string FormatFileSize(this long bytes)
        {
            var unit = 1024;
            if (bytes < unit) { return $"{bytes} B"; }

            var exp = (int)(Math.Log(bytes) / Math.Log(unit));
            return $"{bytes / Math.Pow(unit, exp):F2} {("KMGTPE")[exp - 1]}B";
        }
    }
}
