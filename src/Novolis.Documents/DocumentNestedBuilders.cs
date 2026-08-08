namespace Novolis.Documents;

/// <summary>Fluent builder for <see cref="DocumentMeta"/>.</summary>
public sealed class DocumentMetaBuilder
{
    string _title = "Untitled";
    string? _subtitle;
    string? _series;
    string? _author;
    string? _rights;

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

    /// <summary>Rights.</summary>
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
    public TypographyBuilder TableChrome(float cellPaddingPt, float ruleStrokePt)
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
