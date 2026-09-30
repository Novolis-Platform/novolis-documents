namespace Novolis.Documents;

/// <summary>Fluent builder for <see cref="Footer"/>.</summary>
public sealed class FooterBuilder
{
    string _template = string.Empty;
    float _fontSizePt = 9f;
    bool _includeFirstPage;
    bool _includeToc;
    bool _includeBody = true;
    bool _includeLastPage;

    /// <summary>Footer template.</summary>
    public FooterBuilder Template(string template)
    {
        ArgumentNullException.ThrowIfNull(template);
        _template = template;
        return this;
    }

    /// <summary>Font size in points.</summary>
    public FooterBuilder FontSize(float points)
    {
        _fontSizePt = points;
        return this;
    }

    /// <summary>Include on opening / title page(s).</summary>
    public FooterBuilder IncludeFirstPage(bool include = true)
    {
        _includeFirstPage = include;
        return this;
    }

    /// <summary>Include on TOC pages.</summary>
    public FooterBuilder IncludeToc(bool include = true)
    {
        _includeToc = include;
        return this;
    }

    /// <summary>Include on body pages.</summary>
    public FooterBuilder IncludeBody(bool include = true)
    {
        _includeBody = include;
        return this;
    }

    /// <summary>Include on closing page(s).</summary>
    public FooterBuilder IncludeLastPage(bool include = true)
    {
        _includeLastPage = include;
        return this;
    }

    /// <summary>Builds the footer.</summary>
    public Footer Build() => new()
    {
        Template = _template,
        FontSizePt = _fontSizePt,
        IncludeFirstPage = _includeFirstPage,
        IncludeToc = _includeToc,
        IncludeBody = _includeBody,
        IncludeLastPage = _includeLastPage,
    };
}
