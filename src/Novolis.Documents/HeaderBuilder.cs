namespace Novolis.Documents;

/// <summary>Fluent builder for <see cref="Header"/>.</summary>
public sealed class HeaderBuilder
{
    string _template = string.Empty;
    float _fontSizePt = 9f;
    bool _includeFirstPage;
    bool _includeToc;
    bool _includeBody = true;
    bool _includeLastPage;
    bool _useChapterTitle;

    /// <summary>Header template.</summary>
    public HeaderBuilder Template(string template)
    {
        ArgumentNullException.ThrowIfNull(template);
        _template = template;
        return this;
    }

    /// <summary>Font size in points.</summary>
    public HeaderBuilder FontSize(float points)
    {
        _fontSizePt = points;
        return this;
    }

    /// <summary>Include on opening / title page(s).</summary>
    public HeaderBuilder IncludeFirstPage(bool include = true)
    {
        _includeFirstPage = include;
        return this;
    }

    /// <summary>Include on TOC pages.</summary>
    public HeaderBuilder IncludeToc(bool include = true)
    {
        _includeToc = include;
        return this;
    }

    /// <summary>Include on body pages.</summary>
    public HeaderBuilder IncludeBody(bool include = true)
    {
        _includeBody = include;
        return this;
    }

    /// <summary>Include on closing page(s).</summary>
    public HeaderBuilder IncludeLastPage(bool include = true)
    {
        _includeLastPage = include;
        return this;
    }

    /// <summary>
    /// Use the current chapter title as the header on body pages
    /// (falls back to <see cref="Template"/> when no chapter is active).
    /// </summary>
    public HeaderBuilder UseChapterTitle(bool enabled = true)
    {
        _useChapterTitle = enabled;
        return this;
    }

    /// <summary>Builds the header.</summary>
    public Header Build() => new()
    {
        Template = _template,
        FontSizePt = _fontSizePt,
        IncludeFirstPage = _includeFirstPage,
        IncludeToc = _includeToc,
        IncludeBody = _includeBody,
        IncludeLastPage = _includeLastPage,
        UseChapterTitle = _useChapterTitle,
    };
}
