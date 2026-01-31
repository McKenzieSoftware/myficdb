using System.Text.RegularExpressions;

namespace MyFicDB.Web.IntegrationTests.Infrastructure
{
    public static class AntiforgeryHelper
    {
        // Matches: <input name="__RequestVerificationToken" type="hidden" value="...">
        private static readonly Regex TokenRegex = new(@"name=""__RequestVerificationToken"".*?value=""([^""]+)""", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>
        /// Used when an endpoint has a get page that renders a form (create/edit pages)
        /// </summary>
        public static async Task<string> GetRequestVerificationTokenAsync(HttpClient client, string url)
        {
            var res = await client.GetAsync(url);
            res.EnsureSuccessStatusCode();

            var html = await res.Content.ReadAsStringAsync();
            var match = TokenRegex.Match(html);

            if (!match.Success)
            {
                throw new InvalidOperationException("Could not find __RequestVerificationToken in the HTML response.");
            }

            return match.Groups[1].Value;
        }

        /// <summary>
        /// Use for POST-only endpoints such as deleting, and fetches a token from a known safe page.
        /// </summary>
        public static async Task<string> GetTokenAsync(HttpClient client)
        {
            // this can be any page that is a GET, returns HTML and contains a form with AF
            var res = await client.GetAsync("/story/create");
            res.EnsureSuccessStatusCode();

            return await ExtractTokenAsync(res);
        }

        private static async Task<string> ExtractTokenAsync(HttpResponseMessage res)
        {
            var html = await res.Content.ReadAsStringAsync();
            var match = TokenRegex.Match(html);

            if (!match.Success)
            {
                throw new InvalidOperationException(
                    "Could not find __RequestVerificationToken in the HTML response.");
            }

            return match.Groups[1].Value;
        }
    }
}
