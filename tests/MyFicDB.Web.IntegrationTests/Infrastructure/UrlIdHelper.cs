using System.Text.RegularExpressions;

namespace MyFicDB.Web.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Helper for parsing the GUID for things like /story/{guid:id}
    /// </summary>
    public static class UrlIdHelper
    {
        private static readonly Regex GuidRegex = new(@"([0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12})", RegexOptions.Compiled);

        public static Guid ExtractGuidFromLocation(Uri? location)
        {
            if (location is null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            var match = GuidRegex.Match(location.ToString());
            if (!match.Success)
            {
                throw new InvalidOperationException($"Could not parse GUID from redirect location: {location}");
            }

            return Guid.Parse(match.Groups[1].Value);
        }
    }
}
