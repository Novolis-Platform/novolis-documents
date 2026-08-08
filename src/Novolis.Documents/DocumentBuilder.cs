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
    DocumentMeta? _meta;
    Size _trim = TrimPresets.Inch6x9;
    Thickness _margin = TrimPresets.DefaultMargin;
    Length _headerBand = LengthUnits.FromPoints(16f);
    Length _footerBand = LengthUnits.FromPoints(16f);
    RunningChrome? _header;
    RunningChrome? _footer;
    ChromeOptions _chrome = ChromeOptions.Default;
    Watermark? _watermark;
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
        if (_meta is not null)
            meta.From(_meta);
        else if (_title is not null)
            meta.Title(_title);
        configure(meta);
        _meta = meta.Build();
        _title = _meta.Title;
        return this;
    }

    /// <summary>Configures trim, margins, header, footer, and chrome visibility.</summary>
    public DocumentBuilder Page(Action<DocumentPageBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var page = new DocumentPageBuilder(
            _trim, _margin, _headerBand, _footerBand, _header, _footer, _chrome);
        configure(page);
        _trim = page.TrimValue;
        _margin = page.MarginValue;
        _headerBand = page.HeaderBandValue;
        _footerBand = page.FooterBandValue;
        _header = page.HeaderChrome;
        _footer = page.FooterChrome;
        _chrome = page.ChromeValue;
        return this;
    }

    /// <summary>Diagonal text watermark.</summary>
    public DocumentBuilder Watermark(Action<WatermarkBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new WatermarkBuilder();
        configure(builder);
        _watermark = builder.Build();
        return this;
    }

    /// <summary>Clears any watermark.</summary>
    public DocumentBuilder NoWatermark()
    {
        _watermark = null;
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
        if (string.IsNullOrWhiteSpace(_title) && _meta is null)
            throw new InvalidOperationException("Document requires Title (via Create(title) or Meta) before Build().");

        var meta = _meta;
        if (meta is null)
        {
            meta = new DocumentMeta { Title = _title! };
        }
        else if (!string.IsNullOrWhiteSpace(_title) && !string.Equals(meta.Title, _title, StringComparison.Ordinal))
        {
            meta = new DocumentMetaBuilder().From(meta).Title(_title!).Build();
        }

        return new PagedDocument
        {
            Meta = meta,
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
            Chrome = _chrome,
            Watermark = _watermark,
            First = _body.FirstPage,
            Last = _body.LastPage,
            IncludeCover = _body.IncludeCover,
            IncludeToc = _body.IncludeToc,
            SuppressHeaderOnLevel1Open = _suppressHeaderOnLevel1Open,
            Body = _body.ContentBlocks,
        };
    }
}
