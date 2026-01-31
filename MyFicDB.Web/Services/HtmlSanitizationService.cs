using Ganss.Xss;

namespace MyFicDB.Web.Services
{
    /// <summary>
    /// Provides a centralized HTML sanitization service that strips or restricts unwanted tags,
    /// attributes, and styles from user-generated content to prevent XSS
    /// </summary>
    public class HtmlSanitizationService
    {
        private readonly HtmlSanitizer _sanitizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="HtmlSanitizationService"/> class
        /// with a predefined set of safe tags, attributes, and CSS properties.
        /// </summary>
        public HtmlSanitizationService()
        {
            _sanitizer = new HtmlSanitizer();

            // .AllowedTags, .AllowedAttributes, and .AllowedCssProperties are read-only properties so need to set directly
            _sanitizer.AllowedTags.Clear();
            _sanitizer.AllowedTags.Add("p");
            _sanitizer.AllowedTags.Add("strong");
            _sanitizer.AllowedTags.Add("em");
            _sanitizer.AllowedTags.Add("ul");
            _sanitizer.AllowedTags.Add("ol");
            _sanitizer.AllowedTags.Add("li");
            _sanitizer.AllowedTags.Add("blockquote");
            _sanitizer.AllowedTags.Add("a");
            _sanitizer.AllowedTags.Add("span");
            _sanitizer.AllowedTags.Add("br");
            _sanitizer.AllowedTags.Add("hr");
            _sanitizer.AllowedTags.Add("strike");
            _sanitizer.AllowedTags.Add("sub");
            _sanitizer.AllowedTags.Add("sup");
            _sanitizer.AllowedTags.Add("code");

            _sanitizer.AllowedAttributes.Clear();
            _sanitizer.AllowedAttributes.Add("href");
            _sanitizer.AllowedAttributes.Add("title");
            _sanitizer.AllowedAttributes.Add("style");

            _sanitizer.AllowedCssProperties.Clear();
            _sanitizer.AllowedCssProperties.Add("text-align");

        }

        /// <summary>
        /// Sanitizes the given HTML input by removing any disallowed tags, attributes, or styles.
        /// </summary>
        /// <param name="html">The raw HTML input to sanitize.</param>
        /// <returns>A cleaned and safe HTML string.</returns>
        public string Sanitize(string html)
        {
            return _sanitizer.Sanitize(html);
        }
    }
}
