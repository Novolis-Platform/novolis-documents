namespace Novolis.Documents;

/// <summary>Fluent builder for <see cref="DocumentMeta"/>.</summary>
public sealed class DocumentMetaBuilder
{
    string _title = "Untitled";
    string? _subtitle;
    string? _series;
    string? _author;
    string? _contributors;
    string? _publisher;
    string? _subject;
    string? _description;
    readonly List<string> _keywords = [];
    string? _identifier;
    string? _language;
    string? _version;
    DateOnly? _date;
    string? _rights;

    /// <summary>Seeds from an existing meta snapshot.</summary>
    public DocumentMetaBuilder From(DocumentMeta meta)
    {
        ArgumentNullException.ThrowIfNull(meta);
        _title = meta.Title;
        _subtitle = meta.Subtitle;
        _series = meta.Series;
        _author = meta.Author;
        _contributors = meta.Contributors;
        _publisher = meta.Publisher;
        _subject = meta.Subject;
        _description = meta.Description;
        _keywords.Clear();
        _keywords.AddRange(meta.Keywords);
        _identifier = meta.Identifier;
        _language = meta.Language;
        _version = meta.Version;
        _date = meta.Date;
        _rights = meta.Rights;
        return this;
    }

    /// <summary>Primary title.</summary>
    public DocumentMetaBuilder Title(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        _title = title;
        return this;
    }

    /// <summary>Subtitle.</summary>
    public DocumentMetaBuilder Subtitle(string? subtitle)
    {
        _subtitle = subtitle;
        return this;
    }

    /// <summary>Series.</summary>
    public DocumentMetaBuilder Series(string? series)
    {
        _series = series;
        return this;
    }

    /// <summary>Author.</summary>
    public DocumentMetaBuilder Author(string? author)
    {
        _author = author;
        return this;
    }

    /// <summary>Contributors.</summary>
    public DocumentMetaBuilder Contributors(string? contributors)
    {
        _contributors = contributors;
        return this;
    }

    /// <summary>Publisher / imprint.</summary>
    public DocumentMetaBuilder Publisher(string? publisher)
    {
        _publisher = publisher;
        return this;
    }

    /// <summary>Subject / topic.</summary>
    public DocumentMetaBuilder Subject(string? subject)
    {
        _subject = subject;
        return this;
    }

    /// <summary>Description / abstract.</summary>
    public DocumentMetaBuilder Description(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>Keywords (replaces prior list).</summary>
    public DocumentMetaBuilder Keywords(params string[] keywords)
    {
        ArgumentNullException.ThrowIfNull(keywords);
        _keywords.Clear();
        _keywords.AddRange(keywords.Where(static k => !string.IsNullOrWhiteSpace(k)));
        return this;
    }

    /// <summary>Document identifier (ISBN, DOI, …).</summary>
    public DocumentMetaBuilder Identifier(string? identifier)
    {
        _identifier = identifier;
        return this;
    }

    /// <summary>Language tag.</summary>
    public DocumentMetaBuilder Language(string? language)
    {
        _language = language;
        return this;
    }

    /// <summary>Edition / version label.</summary>
    public DocumentMetaBuilder Version(string? version)
    {
        _version = version;
        return this;
    }

    /// <summary>Publication or issue date.</summary>
    public DocumentMetaBuilder Date(DateOnly? date)
    {
        _date = date;
        return this;
    }

    /// <summary>Rights / copyright.</summary>
    public DocumentMetaBuilder Rights(string? rights)
    {
        _rights = rights;
        return this;
    }

    /// <summary>Builds metadata.</summary>
    public DocumentMeta Build() => new()
    {
        Title = _title,
        Subtitle = _subtitle,
        Series = _series,
        Author = _author,
        Contributors = _contributors,
        Publisher = _publisher,
        Subject = _subject,
        Description = _description,
        Keywords = _keywords.ToArray(),
        Identifier = _identifier,
        Language = _language,
        Version = _version,
        Date = _date,
        Rights = _rights,
    };
}

/// <summary>Fluent builder for <see cref="Typography"/>.</summary>
public sealed class TypographyBuilder
{
    readonly Typography _defaults = new();
    string? _bodyFontFamily;
    float? _bodyFontSizePt;
    float? _h1;
    float? _h2;
    float? _h3;
    float? _sceneBreak;
    float? _tableFont;
    float? _lineHeight;
    float? _paragraphSpacing;
    float? _afterLevel1;
    float? _afterHeading;
    float? _tablePadding;
    float? _tableStroke;

    /// <summary>Body font family name.</summary>
    public TypographyBuilder BodyFontFamily(string family)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(family);
        _bodyFontFamily = family;
        return this;
    }

    /// <summary>Body size in points.</summary>
    public TypographyBuilder BodySize(float points)
    {
        _bodyFontSizePt = points;
        return this;
    }

    /// <summary>Heading sizes in points.</summary>
    public TypographyBuilder HeadingSizes(float h1, float h2, float h3)
    {
        _h1 = h1;
        _h2 = h2;
        _h3 = h3;
        return this;
    }

    /// <summary>Table font size in points.</summary>
    public TypographyBuilder TableSize(float points)
    {
        _tableFont = points;
        return this;
    }

    /// <summary>Line height multiplier.</summary>
    public TypographyBuilder LineHeight(float multiplier)
    {
        _lineHeight = multiplier;
        return this;
    }

    /// <summary>Spacing between blocks in points.</summary>
    public TypographyBuilder ParagraphSpacing(float points)
    {
        _paragraphSpacing = points;
        return this;
    }

    /// <summary>Spacing after headings.</summary>
    public TypographyBuilder AfterHeading(float level1Points, float otherPoints)
    {
        _afterLevel1 = level1Points;
        _afterHeading = otherPoints;
        return this;
    }

    /// <summary>Table cell padding and rule stroke.</summary>
    public TypographyBuilder TableCells(float cellPaddingPt, float ruleStrokePt)
    {
        _tablePadding = cellPaddingPt;
        _tableStroke = ruleStrokePt;
        return this;
    }

    /// <summary>Scene-break ornament size.</summary>
    public TypographyBuilder SceneBreakSize(float points)
    {
        _sceneBreak = points;
        return this;
    }

    /// <summary>Builds typography.</summary>
    public Typography Build() => new()
    {
        BodyFontFamily = _bodyFontFamily ?? _defaults.BodyFontFamily,
        BodyFontSizePt = _bodyFontSizePt ?? _defaults.BodyFontSizePt,
        H1SizePt = _h1 ?? _defaults.H1SizePt,
        H2SizePt = _h2 ?? _defaults.H2SizePt,
        H3SizePt = _h3 ?? _defaults.H3SizePt,
        SceneBreakSizePt = _sceneBreak ?? _defaults.SceneBreakSizePt,
        TableFontSizePt = _tableFont ?? _defaults.TableFontSizePt,
        LineHeight = _lineHeight ?? _defaults.LineHeight,
        ParagraphSpacingPt = _paragraphSpacing ?? _defaults.ParagraphSpacingPt,
        AfterLevel1SpacingPt = _afterLevel1 ?? _defaults.AfterLevel1SpacingPt,
        AfterHeadingSpacingPt = _afterHeading ?? _defaults.AfterHeadingSpacingPt,
        TableCellPaddingPt = _tablePadding ?? _defaults.TableCellPaddingPt,
        TableRuleStrokePt = _tableStroke ?? _defaults.TableRuleStrokePt,
    };
}

/// <summary>Fluent builder for <see cref="FirstPage"/>.</summary>
public sealed class FirstPageBuilder
{
    string? _title;
    string? _subtitle;
    string? _series;
    string? _author;
    string? _rights;
    readonly List<string> _lines = [];

    /// <summary>Title override.</summary>
    public FirstPageBuilder Title(string? title)
    {
        _title = title;
        return this;
    }

    /// <summary>Subtitle override.</summary>
    public FirstPageBuilder Subtitle(string? subtitle)
    {
        _subtitle = subtitle;
        return this;
    }

    /// <summary>Series override.</summary>
    public FirstPageBuilder Series(string? series)
    {
        _series = series;
        return this;
    }

    /// <summary>Author override.</summary>
    public FirstPageBuilder Author(string? author)
    {
        _author = author;
        return this;
    }

    /// <summary>Rights override.</summary>
    public FirstPageBuilder Rights(string? rights)
    {
        _rights = rights;
        return this;
    }

    /// <summary>Additional centered lines.</summary>
    public FirstPageBuilder Lines(params string[] lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        _lines.AddRange(lines);
        return this;
    }

    /// <summary>Builds the first page.</summary>
    public FirstPage Build() => new()
    {
        Title = _title,
        Subtitle = _subtitle,
        Series = _series,
        Author = _author,
        Rights = _rights,
        Lines = _lines.ToArray(),
    };
}

/// <summary>Fluent builder for <see cref="LastPage"/>.</summary>
public sealed class LastPageBuilder
{
    string? _title;
    readonly List<string> _lines = [];
    readonly DocumentContentBuilder _blocks = new();

    /// <summary>Closing title.</summary>
    public LastPageBuilder Title(string? title)
    {
        _title = title;
        return this;
    }

    /// <summary>Plain closing lines.</summary>
    public LastPageBuilder Lines(params string[] lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        _lines.AddRange(lines);
        return this;
    }

    /// <summary>Richer blocks after lines.</summary>
    public LastPageBuilder Blocks(Action<DocumentContentBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_blocks);
        return this;
    }

    /// <summary>Builds the last page.</summary>
    public LastPage Build() => new()
    {
        Title = _title,
        Lines = _lines.ToArray(),
        Blocks = _blocks.ToBlocks(),
    };
}

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

    /// <summary>Include on the opening / title page.</summary>
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

    /// <summary>Include on the closing page.</summary>
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

/// <summary>Fluent builder for <see cref="Footer"/>.</summary>
public sealed class FooterBuilder
{
    string _template = string.Empty;
    float _fontSizePt = 9f;
    bool _includeFirstPage = true;
    bool _includeToc = true;
    bool _includeBody = true;
    bool _includeLastPage = true;

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

    /// <summary>Include on the opening / title page.</summary>
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

    /// <summary>Include on the closing page.</summary>
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

/// <summary>Fluent builder for <see cref="Watermark"/>.</summary>
public sealed class WatermarkBuilder
{
    string _text = "DRAFT";
    float _fontSizePt = 54f;
    float _opacity = 0.12f;
    DocumentColor _color = DocumentColor.Red;
    float _rotation = -32f;
    WatermarkPages _pages = WatermarkPages.All;

    /// <summary>Watermark text.</summary>
    public WatermarkBuilder Text(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        _text = text;
        return this;
    }

    /// <summary>Font size in points.</summary>
    public WatermarkBuilder FontSize(float points)
    {
        _fontSizePt = points;
        return this;
    }

    /// <summary>Opacity 0–1.</summary>
    public WatermarkBuilder Opacity(float opacity)
    {
        _opacity = opacity;
        return this;
    }

    /// <summary>Ink color (prefer named colors such as <see cref="DocumentColor.Red"/>).</summary>
    public WatermarkBuilder Color(DocumentColor color)
    {
        _color = color;
        return this;
    }

    /// <summary>Rotation in degrees.</summary>
    public WatermarkBuilder Rotation(float degrees)
    {
        _rotation = degrees;
        return this;
    }

    /// <summary>Which regions show the watermark.</summary>
    public WatermarkBuilder On(WatermarkPages pages)
    {
        _pages = pages;
        return this;
    }

    /// <summary>Builds the watermark.</summary>
    public Watermark Build() => new()
    {
        Text = _text,
        FontSizePt = _fontSizePt,
        Opacity = _opacity,
        Color = _color,
        RotationDegrees = _rotation,
        Pages = _pages,
    };
}
