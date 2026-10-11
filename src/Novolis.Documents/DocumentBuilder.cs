using Novolis.Math.Measure;

namespace Novolis.Documents;

/// <summary>
/// Fluent construction DSL for the semantic <see cref="Document"/> model.
/// </summary>
public sealed class DocumentBuilder
{
    string? _title;
    DocumentMeta? _meta;
    Size _trim = TrimPresets.Inch6x9;
    Thickness _margin = TrimPresets.DefaultMargin;
    Length _headerBand = LengthUnits.FromPoints(16f);
    Length _footerBand = LengthUnits.FromPoints(16f);
    Header? _header;
    Footer? _footer;
    Watermark? _watermark;
    Typography? _typography;
    DocumentTheme? _theme;
    readonly DocumentBodyBuilder _legacyBody = new();
    readonly List<ISection> _sections = [];
    PageDefinition? _firstPage;
    PageDefinition? _lastPage;
    bool _includeToc;

    /// <summary>Primary document title.</summary>
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

    /// <summary>Configures trim, margins, header, and footer.</summary>
    public DocumentBuilder Page(Action<DocumentPageBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var page = new DocumentPageBuilder(
            _trim, _margin, _headerBand, _footerBand, _header, _footer);
        configure(page);
        _trim = page.TrimValue;
        _margin = page.MarginValue;
        _headerBand = page.HeaderBandValue;
        _footerBand = page.FooterBandValue;
        _header = page.HeaderValue;
        _footer = page.FooterValue;
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

    /// <summary>Configures document-wide semantic theme defaults.</summary>
    public DocumentBuilder Theme(DocumentTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        _theme = theme;
        _typography = theme.Typography;
        return this;
    }

    /// <summary>Adds a normal vertical section.</summary>
    public DocumentBuilder Section(Action<SectionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new SectionBuilder();
        configure(builder);
        _sections.Add(builder.Build());
        return this;
    }

    /// <summary>Adds an already-built section.</summary>
    public DocumentBuilder Add(ISection section)
    {
        ArgumentNullException.ThrowIfNull(section);
        _sections.Add(section);
        return this;
    }

    /// <summary>Adds a horizontal columns section.</summary>
    public DocumentBuilder Columns(Action<ColumnsSectionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new ColumnsSectionBuilder();
        configure(builder);
        _sections.Add(builder.Build());
        return this;
    }

    /// <summary>Adds a table section.</summary>
    public DocumentBuilder Table(Action<SemanticTableBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new SemanticTableBuilder();
        configure(builder);
        _sections.Add(builder.Build());
        return this;
    }

    /// <summary>Configures a dedicated first page.</summary>
    public DocumentBuilder FirstPage(Action<PageDefinitionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new PageDefinitionBuilder();
        configure(builder);
        _firstPage = builder.Build();
        return this;
    }

    /// <summary>Configures a dedicated last page.</summary>
    public DocumentBuilder LastPage(Action<PageDefinitionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new PageDefinitionBuilder();
        configure(builder);
        _lastPage = builder.Build();
        return this;
    }

    /// <summary>Collects level-one headings into the PDF outline.</summary>
    public DocumentBuilder Toc(bool include = true)
    {
        _includeToc = include;
        return this;
    }

    /// <summary>
    /// Retains the earlier body-spine authoring shape while callers migrate to
    /// <see cref="Section(Action{SectionBuilder})"/>.
    /// </summary>
    public DocumentBuilder Body(Action<DocumentBodyBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_legacyBody);
        return this;
    }

    /// <summary>Materializes an immutable semantic document.</summary>
    public Document Build()
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

        var sections = new List<ISection>(_sections);
        sections.AddRange(LegacyBlockMapper.ToSections(_legacyBody.ContentBlocks));

        var first = _firstPage;
        if (first is null && _legacyBody.FirstPage is { } legacyFirst)
            first = LegacyBlockMapper.ToFirstPage(legacyFirst, meta);

        var last = _lastPage;
        if (last is null && _legacyBody.LastPage is { } legacyLast)
            last = LegacyBlockMapper.ToLastPage(legacyLast);

        return new Document
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
            Watermark = _watermark,
            Theme = _theme ?? new DocumentTheme { Typography = _typography ?? new Typography() },
            FirstPage = first,
            LastPage = last,
            IncludeCover = _legacyBody.IncludeCover || first is not null,
            Body = _legacyBody.ContentBlocks,
            IncludeToc = _includeToc || _legacyBody.IncludeToc,
            Sections = sections.ToArray(),
        };
    }
}
