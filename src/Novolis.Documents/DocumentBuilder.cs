using Novolis.Math.Measure;

namespace Novolis.Documents;

/// <summary>
/// Fluent construction DSL for <see cref="PagedDocument"/> (P1 shape):
/// <c>Document.Create → Meta / Page → Body { First, Content, Last }</c>.
/// Assembles the immutable block model — not a constraint layout engine.
/// </summary>
public sealed class DocumentBuilder
{
    string? _title;
    string? _subtitle;
    string? _series;
    string? _author;
    string? _rights;
    Size _trim = TrimPresets.Inch6x9;
    Thickness _margin = TrimPresets.DefaultMargin;
    Length _headerBand = LengthUnits.FromPoints(16f);
    Length _footerBand = LengthUnits.FromPoints(16f);
    RunningChrome? _header;
    RunningChrome? _footer;
    Typography? _typography;
    bool _suppressHeaderOnLevel1Open = true;
    readonly DocumentBodyBuilder _body = new();

    /// <summary>Primary document title (required for <see cref="Build"/>).</summary>
    public DocumentBuilder Title(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        _title = title;
        return this;
    }

    /// <summary>Configures metadata.</summary>
    public DocumentBuilder Meta(Action<DocumentMetaBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var meta = new DocumentMetaBuilder();
        if (_title is not null)
            meta.Title(_title);
        if (_subtitle is not null)
            meta.Subtitle(_subtitle);
        if (_series is not null)
            meta.Series(_series);
        if (_author is not null)
            meta.Author(_author);
        if (_rights is not null)
            meta.Rights(_rights);
        configure(meta);
        var built = meta.Build();
        _title = built.Title;
        _subtitle = built.Subtitle;
        _series = built.Series;
        _author = built.Author;
        _rights = built.Rights;
        return this;
    }

    /// <summary>Configures trim, margins, header, and footer.</summary>
    public DocumentBuilder Page(Action<DocumentPageBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var page = new DocumentPageBuilder(_trim, _margin, _headerBand, _footerBand, _header, _footer);
        configure(page);
        _trim = page.Trim;
        _margin = page.Margin;
        _headerBand = page.HeaderBand;
        _footerBand = page.FooterBand;
        _header = page.HeaderChrome;
        _footer = page.FooterChrome;
        return this;
    }

    /// <summary>Configures typography.</summary>
    public DocumentBuilder Typography(Action<TypographyBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new TypographyBuilder();
        configure(builder);
        _typography = builder.Build();
        return this;
    }

    /// <summary>Replaces typography with a finished instance.</summary>
    public DocumentBuilder Typography(Typography typography)
    {
        ArgumentNullException.ThrowIfNull(typography);
        _typography = typography;
        return this;
    }

    /// <summary>
    /// Configures the document body spine: <see cref="DocumentBodyBuilder.First"/>,
    /// <see cref="DocumentBodyBuilder.Content"/>, <see cref="DocumentBodyBuilder.Last"/>.
    /// </summary>
    public DocumentBuilder Body(Action<DocumentBodyBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_body);
        return this;
    }

    /// <summary>Suppress header on pages that open with a level-1 heading / chapter.</summary>
    public DocumentBuilder SuppressHeaderOnLevel1Open(bool suppress = true)
    {
        _suppressHeaderOnLevel1Open = suppress;
        return this;
    }

    /// <summary>Materializes an immutable <see cref="PagedDocument"/>.</summary>
    public PagedDocument Build()
    {
        if (string.IsNullOrWhiteSpace(_title))
            throw new InvalidOperationException("Document requires Title (via Create(title) or Meta) before Build().");

        return new PagedDocument
        {
            Meta = new DocumentMeta
            {
                Title = _title,
                Subtitle = _subtitle,
                Series = _series,
                Author = _author,
                Rights = _rights,
            },
            Setup = new PageSetup
            {
                Trim = _trim,
                Margin = _margin,
                HeaderBand = _headerBand,
                FooterBand = _footerBand,
            },
            Typography = _typography ?? new Typography(),
            Header = _header,
            Footer = _footer,
            First = _body.FirstPage,
            Last = _body.LastPage,
            IncludeCover = _body.IncludeCover,
            IncludeToc = _body.IncludeToc,
            SuppressHeaderOnLevel1Open = _suppressHeaderOnLevel1Open,
            Body = _body.ContentBlocks,
        };
    }
}
