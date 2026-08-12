using System.Text;

namespace EvitaDB.Storefront.Services;

/// <summary>
/// Turns the rich HTML stored in the `description` attribute into something safe to hand to
/// <c>MarkupString</c>.
///
/// The stored markup is a whole marketing page lifted from a vendor site: it carries that site's class
/// names, `data-analytics-*` hooks, and buttons wired to modals this app does not have. Rendering it raw
/// would both look broken and inject arbitrary markup into the page, so the content is rewritten against
/// an allowlist:
///
/// <list type="bullet">
///   <item>elements not on the allowlist are <b>unwrapped</b> - their text survives, the tag does not;</item>
///   <item><c>script</c>, <c>style</c>, <c>iframe</c>, <c>object</c>, <c>embed</c>, <c>form</c>,
///         <c>input</c> and <c>button</c> are dropped <b>with their content</b> (a modal trigger with no
///         modal behind it is worse than nothing);</item>
///   <item>every attribute is stripped except <c>href</c>, <c>title</c>, <c>alt</c> and <c>src</c>, and
///         those only when the value is not a scripting URL.</item>
/// </list>
///
/// This is a deliberately small hand-rolled sanitizer rather than a dependency: the demo has no other
/// need for one, and its rules are easier to audit inline than to configure.
/// </summary>
public static class HtmlContent
{
    /// <summary>Structural and inline elements worth keeping; everything else is unwrapped.</summary>
    private static readonly HashSet<string> AllowedElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "section", "article", "div", "p", "span", "br", "hr",
        "h1", "h2", "h3", "h4", "h5", "h6",
        "ul", "ol", "li", "dl", "dt", "dd",
        "strong", "b", "em", "i", "u", "small", "sup", "sub", "mark", "abbr",
        "a", "img", "figure", "figcaption", "blockquote", "cite", "code", "pre",
        "table", "thead", "tbody", "tfoot", "tr", "td", "th", "caption"
    };

    /// <summary>Elements removed together with everything inside them.</summary>
    private static readonly HashSet<string> DroppedWithContent = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "iframe", "object", "embed", "form", "input", "select", "textarea",
        "button", "noscript", "svg", "canvas", "video", "audio"
    };

    private static readonly HashSet<string> VoidElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "br", "hr", "img", "input", "meta", "link", "source", "col"
    };

    private static readonly HashSet<string> AllowedAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "href", "title", "alt", "src"
    };

    /// <summary>True when the value carries any renderable text at all.</summary>
    public static bool HasContent(string? html) => !string.IsNullOrWhiteSpace(StripTags(html));

    /// <summary>Plain text of the markup - used for teasers and meta descriptions.</summary>
    public static string StripTags(string? html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return string.Empty;
        }
        StringBuilder text = new();
        int index = 0;
        while (index < html.Length)
        {
            if (html[index] == '<')
            {
                int close = html.IndexOf('>', index);
                if (close < 0) break;
                index = close + 1;
                // a tag boundary is a word boundary - without this, "iPad<br/>s novým" became "iPads novým"
                text.Append(' ');
                continue;
            }
            text.Append(html[index++]);
        }
        return CollapseWhitespace(DecodeEntities(text.ToString()));
    }

    /// <summary>Sanitized markup, ready for <c>MarkupString</c>.</summary>
    public static string Sanitize(string? html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return string.Empty;
        }

        StringBuilder output = new(html.Length);
        // when > 0 we are inside a dropped element and everything is discarded until it closes
        int suppressDepth = 0;
        string? suppressedElement = null;
        int index = 0;

        while (index < html.Length)
        {
            char current = html[index];
            if (current != '<')
            {
                if (suppressDepth == 0)
                {
                    output.Append(current);
                }
                index++;
                continue;
            }

            int close = html.IndexOf('>', index);
            if (close < 0)
            {
                // unterminated tag - treat the remainder as text so nothing is silently lost
                if (suppressDepth == 0)
                {
                    output.Append(System.Net.WebUtility.HtmlEncode(html[index..]));
                }
                break;
            }

            string tag = html[(index + 1)..close].Trim();
            index = close + 1;

            if (tag.StartsWith('!'))
            {
                continue; // comments and doctypes
            }

            bool isClosing = tag.StartsWith('/');
            string name = ElementName(isClosing ? tag[1..] : tag);
            if (name.Length == 0)
            {
                continue;
            }

            if (suppressDepth > 0)
            {
                // only the element that started the suppression can end it
                if (name.Equals(suppressedElement, StringComparison.OrdinalIgnoreCase))
                {
                    if (isClosing)
                    {
                        suppressDepth--;
                        if (suppressDepth == 0) suppressedElement = null;
                    }
                    else if (!IsSelfClosing(tag, name))
                    {
                        suppressDepth++;
                    }
                }
                continue;
            }

            if (DroppedWithContent.Contains(name))
            {
                if (!isClosing && !IsSelfClosing(tag, name))
                {
                    suppressDepth = 1;
                    suppressedElement = name;
                }
                continue;
            }

            if (!AllowedElements.Contains(name))
            {
                continue; // unwrap: the tag disappears, its content stays
            }

            if (isClosing)
            {
                if (!VoidElements.Contains(name))
                {
                    output.Append("</").Append(name.ToLowerInvariant()).Append('>');
                }
                continue;
            }

            output.Append('<').Append(name.ToLowerInvariant());
            AppendSafeAttributes(output, tag, name);
            output.Append(VoidElements.Contains(name) ? "/>" : ">");
        }

        return output.ToString();
    }

    private static void AppendSafeAttributes(StringBuilder output, string tag, string elementName)
    {
        foreach ((string attribute, string value) in ParseAttributes(tag))
        {
            if (!AllowedAttributes.Contains(attribute))
            {
                continue;
            }
            if ((attribute.Equals("href", StringComparison.OrdinalIgnoreCase)
                 || attribute.Equals("src", StringComparison.OrdinalIgnoreCase))
                && !IsSafeUrl(value))
            {
                continue;
            }
            output.Append(' ').Append(attribute.ToLowerInvariant())
                .Append("=\"").Append(System.Net.WebUtility.HtmlEncode(value)).Append('"');
        }
        // links in imported copy point at the vendor's own site - open them away from the storefront
        if (elementName.Equals("a", StringComparison.OrdinalIgnoreCase))
        {
            output.Append(" target=\"_blank\" rel=\"noopener noreferrer nofollow\"");
        }
    }

    /// <summary>Rejects `javascript:`, `data:` and friends while allowing http(s), mailto and relative URLs.</summary>
    private static bool IsSafeUrl(string value)
    {
        string trimmed = value.Trim();
        int colon = trimmed.IndexOf(':');
        int slash = trimmed.IndexOf('/');
        if (colon < 0 || (slash >= 0 && slash < colon))
        {
            return true; // relative
        }
        string scheme = trimmed[..colon];
        return scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
               || scheme.Equals("https", StringComparison.OrdinalIgnoreCase)
               || scheme.Equals("mailto", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<(string Name, string Value)> ParseAttributes(string tag)
    {
        int index = ElementName(tag).Length;
        while (index < tag.Length)
        {
            while (index < tag.Length && char.IsWhiteSpace(tag[index])) index++;
            int nameStart = index;
            while (index < tag.Length && tag[index] != '=' && !char.IsWhiteSpace(tag[index]) && tag[index] != '/')
            {
                index++;
            }
            if (index == nameStart)
            {
                index++;
                continue;
            }
            string name = tag[nameStart..index];

            while (index < tag.Length && char.IsWhiteSpace(tag[index])) index++;
            if (index >= tag.Length || tag[index] != '=')
            {
                yield return (name, string.Empty); // valueless attribute
                continue;
            }
            index++; // '='
            while (index < tag.Length && char.IsWhiteSpace(tag[index])) index++;
            if (index >= tag.Length)
            {
                yield break;
            }

            string value;
            char quote = tag[index];
            if (quote is '"' or '\'')
            {
                index++;
                int valueStart = index;
                while (index < tag.Length && tag[index] != quote) index++;
                value = tag[valueStart..Math.Min(index, tag.Length)];
                index++;
            }
            else
            {
                int valueStart = index;
                while (index < tag.Length && !char.IsWhiteSpace(tag[index])) index++;
                value = tag[valueStart..index];
            }
            yield return (name, value);
        }
    }

    private static string ElementName(string tag)
    {
        int end = 0;
        while (end < tag.Length && (char.IsLetterOrDigit(tag[end]) || tag[end] is '-' or ':'))
        {
            end++;
        }
        return tag[..end];
    }

    private static bool IsSelfClosing(string tag, string name) =>
        tag.EndsWith('/') || VoidElements.Contains(name);

    /// <summary>Decodes entities and normalises the non-breaking spaces this copy is full of.</summary>
    private static string DecodeEntities(string text) =>
        System.Net.WebUtility.HtmlDecode(text).Replace('\u00a0', ' ');

    /// <summary>Collapses the runs of whitespace left behind by stripped tags into single spaces.</summary>
    private static string CollapseWhitespace(string text)
    {
        StringBuilder builder = new(text.Length);
        bool pendingSpace = false;
        foreach (char character in text)
        {
            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }
            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }
            builder.Append(character);
        }
        return builder.ToString();
    }

    /// <summary>Plain-text teaser of at most <paramref name="maxLength"/> characters, cut on a word boundary.</summary>
    public static string Teaser(string? html, int maxLength)
    {
        string text = StripTags(html);
        if (text.Length <= maxLength)
        {
            return text;
        }
        int cut = text.LastIndexOf(' ', Math.Min(maxLength, text.Length - 1));
        return string.Concat(text.AsSpan(0, cut > maxLength / 2 ? cut : maxLength).TrimEnd(), "\u2026");
    }
}
