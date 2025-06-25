using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace FriendlyUrls.Services
{
    /// <summary>
    /// Service for creating URL-friendly slugs from text
    /// </summary>
    public class SlugService
    {
        /// <summary>
        /// Creates a URL-friendly slug from the input text
        /// </summary>
        /// <param name="input">Text to convert to slug</param>
        /// <returns>URL-friendly slug</returns>
        public string CreateSlug(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Normalize text (remove accents)
            var normalizedString = input.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            var slug = stringBuilder.ToString().Normalize(NormalizationForm.FormC);

            // Convert to lowercase
            slug = slug.ToLowerInvariant();

            // Replace special characters
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"-+", "-");
            slug = slug.Trim('-');

            return slug;
        }
    }
}