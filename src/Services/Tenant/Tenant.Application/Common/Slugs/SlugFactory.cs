using System.Globalization;
using System.Text;

namespace Tenant.Application.Common.Slugs;

/// <summary>
/// Turns free-form text into a URL-safe, lowercase slug: strips accents, lowercases, and collapses any
/// run of non-alphanumeric characters into a single hyphen. Kept as a pure function so it is trivially
/// testable; the seam can be promoted to an injectable service if slug policy ever needs to vary.
/// </summary>
public static class SlugFactory
{
    public static string Create(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder(normalized.Length);
        var lastWasHyphen = false;

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen && builder.Length > 0)
            {
                builder.Append('-');
                lastWasHyphen = true;
            }
        }

        return builder.ToString().Trim('-');
    }
}
