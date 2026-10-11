namespace Novolis.Documents;

/// <summary>Builds one vertical section from semantic content.</summary>
public sealed class SectionBuilder
{
    readonly List<IContent> _contents = [];
    SectionLayout _layout = SectionLayout.Default;

    /// <summary>Adds arbitrary content.</summary>
    public SectionBuilder Add(IContent content)
    {
        ArgumentNullException.ThrowIfNull(content);
        _contents.Add(content);
        return this;
    }

    /// <summary>Adds text.</summary>
    public SectionBuilder Text(string value, ContentStyle? style = null) =>
        Add(new Text(value, style));

    /// <summary>Adds a paragraph.</summary>
    public SectionBuilder Paragraph(string value) => Text(value);

    /// <summary>Adds a heading.</summary>
    public SectionBuilder Heading(string value, int level = 1) =>
        Add(new Heading(value, level));

    /// <summary>Adds a level-one heading.</summary>
    public SectionBuilder H1(string value) => Heading(value, 1);

    /// <summary>Adds a level-two heading.</summary>
    public SectionBuilder H2(string value) => Heading(value, 2);

    /// <summary>Adds a level-three heading.</summary>
    public SectionBuilder H3(string value) => Heading(value, 3);

    /// <summary>Adds a level-four heading.</summary>
    public SectionBuilder H4(string value) => Heading(value, 4);

    /// <summary>Adds a semantic number.</summary>
    public SectionBuilder Number(decimal value, NumberFormat? format = null) =>
        Add(new Number(value, format ?? NumberFormat.Decimal()));

    /// <summary>Adds a fixed-size image.</summary>
    public SectionBuilder Image(string path, float widthPt, float heightPt) =>
        Add(new Image(path, widthPt, heightPt));

    /// <summary>Adds a fixed-size byte-backed image.</summary>
    public SectionBuilder Image(byte[] data, float widthPt, float heightPt, bool svg = false) =>
        Add(new Image(data, widthPt, heightPt, svg));

    /// <summary>Adds a horizontal rule.</summary>
    public SectionBuilder Rule() => Add(new Rule());

    /// <summary>Adds vertical space.</summary>
    public SectionBuilder Spacer(float heightPt) => Add(new Spacer(heightPt));

    /// <summary>Adds a key/value pair.</summary>
    public SectionBuilder KeyValue(string key, string value) =>
        Add(new KeyValue(key, value));

    /// <summary>Adds a numeric key/value pair.</summary>
    public SectionBuilder KeyValue(string key, Number value) =>
        Add(new KeyValue(key, value));

    /// <summary>Adds code content.</summary>
    public SectionBuilder Code(string source, string? language = null) =>
        Add(new Code(source, language));

    /// <summary>Adds a nested horizontal composition.</summary>
    public SectionBuilder Columns(Action<ColumnsSectionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new ColumnsSectionBuilder();
        configure(builder);
        return Add(builder.Build());
    }

    /// <summary>Sets section pagination policy.</summary>
    public SectionBuilder Layout(SectionLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        _layout = layout;
        return this;
    }

    /// <summary>Marks this section as keep-together.</summary>
    public SectionBuilder KeepTogether(bool enabled = true) =>
        Layout(_layout with { KeepTogether = enabled });

    /// <summary>Marks this section as keep-with-next.</summary>
    public SectionBuilder KeepWithNext(bool enabled = true) =>
        Layout(_layout with { KeepWithNext = enabled });

    /// <summary>Sets section spacing.</summary>
    public SectionBuilder Spacing(float beforePt, float afterPt = 0f) =>
        Layout(_layout with { SpaceBefore = beforePt, SpaceAfter = afterPt });

    internal Section Build() => new(_contents, _layout);
}

/// <summary>Builds a page definition from sections.</summary>
public sealed class PageDefinitionBuilder
{
    readonly List<ISection> _sections = [];

    /// <summary>Adds a vertical section.</summary>
    public PageDefinitionBuilder Section(Action<SectionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new SectionBuilder();
        configure(builder);
        _sections.Add(builder.Build());
        return this;
    }

    /// <summary>Adds a pre-built section.</summary>
    public PageDefinitionBuilder Add(ISection section)
    {
        ArgumentNullException.ThrowIfNull(section);
        _sections.Add(section);
        return this;
    }

    /// <summary>Adds a columns section.</summary>
    public PageDefinitionBuilder Columns(Action<ColumnsSectionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new ColumnsSectionBuilder();
        configure(builder);
        _sections.Add(builder.Build());
        return this;
    }

    /// <summary>Adds a table section.</summary>
    public PageDefinitionBuilder Table(Action<SemanticTableBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new SemanticTableBuilder();
        configure(builder);
        _sections.Add(builder.Build());
        return this;
    }

    internal PageDefinition Build() => new(_sections);
}

/// <summary>Builds columns inside one document-flow section.</summary>
public sealed class ColumnsSectionBuilder
{
    readonly List<Column> _columns = [];
    float _gapPt = 12f;
    SectionLayout _layout = new() { KeepTogether = true };

    /// <summary>Sets the inter-column gap.</summary>
    public ColumnsSectionBuilder Gap(float gapPt)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(gapPt);
        _gapPt = gapPt;
        return this;
    }

    /// <summary>Adds a weighted column.</summary>
    public ColumnsSectionBuilder Column(Action<SectionBuilder> configure) =>
        Column(new ColumnWidth.Weight(1f), configure);

    /// <summary>Adds a column with an explicit width policy.</summary>
    public ColumnsSectionBuilder Column(ColumnWidth width, Action<SectionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(width);
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new SectionBuilder();
        configure(builder);
        var section = builder.Build();
        _columns.Add(new Column(section.Contents, width));
        return this;
    }

    /// <summary>Adds a pre-built column.</summary>
    public ColumnsSectionBuilder Add(Column column)
    {
        ArgumentNullException.ThrowIfNull(column);
        _columns.Add(column);
        return this;
    }

    /// <summary>Sets columns pagination policy.</summary>
    public ColumnsSectionBuilder Layout(SectionLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        _layout = layout;
        return this;
    }

    internal ColumnsSection Build() => new(_columns, _gapPt, _layout);
}

/// <summary>Builds a semantic table section.</summary>
public sealed class SemanticTableBuilder
{
    readonly List<TableColumn> _columns = [];
    readonly List<TableRow> _rows = [];
    TableRow? _header;
    TableOptions _options = new();
    SectionLayout _layout = SectionLayout.Default;

    /// <summary>Defines table columns.</summary>
    public SemanticTableBuilder Columns(params TableColumn[] columns)
    {
        ArgumentNullException.ThrowIfNull(columns);
        _columns.Clear();
        _columns.AddRange(columns);
        return this;
    }

    /// <summary>Adds one table column.</summary>
    public SemanticTableBuilder Column(ColumnWidth width, HorizontalAlignment alignment = HorizontalAlignment.Left) =>
        Columns([.. _columns, new TableColumn(width, alignment)]);

    /// <summary>Defines a header row.</summary>
    public SemanticTableBuilder Header(params string[] cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        _header = new TableRow(cells);
        _options = _options with { ShowHeader = cells.Length > 0, Header = _header };
        return this;
    }

    /// <summary>Defines a semantic header row.</summary>
    public SemanticTableBuilder Header(params TableCell[] cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        _header = new TableRow(cells);
        _options = _options with { ShowHeader = cells.Length > 0, Header = _header };
        return this;
    }

    /// <summary>Adds a row of text cells.</summary>
    public SemanticTableBuilder Row(params string[] cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        _rows.Add(new TableRow(cells));
        return this;
    }

    /// <summary>Adds a semantic row.</summary>
    public SemanticTableBuilder Row(params TableCell[] cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        _rows.Add(new TableRow(cells));
        return this;
    }

    /// <summary>Adds several rows.</summary>
    public SemanticTableBuilder Rows(IEnumerable<IEnumerable<string>> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        foreach (var row in rows)
            Row(row.ToArray());
        return this;
    }

    /// <summary>Sets rule styling.</summary>
    public SemanticTableBuilder Rules(TableRuleStyle style)
    {
        _options = _options with { RuleStyle = style };
        return this;
    }

    /// <summary>Enables or disables header fill.</summary>
    public SemanticTableBuilder HeaderBackground(bool enabled = true)
    {
        _options = _options with { HeaderBackground = enabled };
        return this;
    }

    /// <summary>Enables or disables repeated headers.</summary>
    public SemanticTableBuilder RepeatHeaderOnPageBreak(bool enabled = true)
    {
        _options = _options with { RepeatHeader = enabled };
        return this;
    }

    /// <summary>Sets cell padding.</summary>
    public SemanticTableBuilder Padding(float paddingPt)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(paddingPt);
        _options = _options with { CellPaddingPt = paddingPt };
        return this;
    }

    /// <summary>Sets table pagination policy.</summary>
    public SemanticTableBuilder Layout(SectionLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        _layout = layout;
        return this;
    }

    /// <summary>Keeps the table together when it fits on an empty page.</summary>
    public SemanticTableBuilder KeepTogether(bool enabled = true) =>
        Layout(_layout with { KeepTogether = enabled });

    internal TableSection Build()
    {
        var columns = _columns.Count > 0
            ? _columns
            : InferColumns(_header, _rows);
        var options = _options with { Header = _header };
        return new TableSection(columns, _rows, options, _layout);
    }

    static IReadOnlyList<TableColumn> InferColumns(TableRow? header, IReadOnlyList<TableRow> rows)
    {
        var count = header?.Cells.Count ?? 0;
        count = System.Math.Max(count, rows.Select(static row => row.Cells.Count).DefaultIfEmpty().Max());
        return Enumerable.Range(0, count)
            .Select(static _ => new TableColumn(new ColumnWidth.Weight(1f)))
            .ToArray();
    }
}

/// <summary>Converts the legacy block model while callers migrate to sections.</summary>
internal static class LegacyBlockMapper
{
    public static IReadOnlyList<ISection> ToSections(IReadOnlyList<IBlock> blocks) =>
        blocks.SelectMany(ToSections).ToArray();

    public static PageDefinition ToFirstPage(FirstPage page, DocumentMeta meta)
    {
        var sections = new List<ISection>
        {
            new Section([new Heading(page.Title ?? meta.Title, 1)]),
        };

        AddText(sections, page.Subtitle ?? meta.Subtitle);
        AddText(sections, page.Series ?? meta.Series);
        AddText(sections, page.Author ?? meta.Author);
        foreach (var line in page.Lines)
            AddText(sections, line);
        AddText(sections, page.Rights ?? meta.Rights);
        sections.AddRange(ToSections(page.Blocks));
        return new PageDefinition(sections);
    }

    public static PageDefinition ToLastPage(LastPage page)
    {
        var sections = new List<ISection>();
        AddText(sections, page.Title);
        foreach (var line in page.Lines)
            AddText(sections, line);
        sections.AddRange(ToSections(page.Blocks));
        return new PageDefinition(sections);
    }

    static void AddText(List<ISection> sections, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            sections.Add(new Section([new Text(value)]));
    }

    static IEnumerable<ISection> ToSections(IBlock block)
    {
        switch (block)
        {
            case ParagraphBlock paragraph:
                yield return new Section([new Text(paragraph.Text)]);
                yield break;
            case HeadingBlock heading:
                yield return new Section(
                    [new Heading(heading.Text, System.Math.Clamp(heading.Level, 1, 4))],
                    new SectionLayout { KeepWithNext = true });
                yield break;
            case TableBlock table:
                yield return ToTable(table);
                yield break;
            case ColumnsBlock columns:
                yield return ToColumns(columns);
                yield break;
            case ImageBlock image:
                yield return new Section([ToImage(image)]);
                yield break;
            case CodeBlock code:
                yield return new Section([new Code(string.Join(
                    Environment.NewLine,
                    code.ResolveLines().Select(static line =>
                        string.Concat(line.Spans.Select(static span => span.Text)))))]);
                yield break;
            case TextBoxBlock box:
                yield return new Box(box.Lines.Select(static line => (IContent)new Text(line)));
                yield break;
            case SceneBreakBlock scene:
                yield return new Section([new Text(scene.Ornament)]);
                yield break;
            case LineBreakBlock:
                yield return new Section([new Spacer(8f)]);
                yield break;
            case PageBreakBlock:
                yield return new PageBreakSection();
                yield break;
            case BlankPageBlock:
                yield return new PageBreakSection();
                yield return new PageBreakSection();
                yield break;
        }
    }

    static Image ToImage(ImageBlock image)
    {
        if (image.Data is { Length: > 0 } data)
            return new Image(data, image.WidthPt, image.HeightPt);
        return new Image(image.Path ?? string.Empty, image.WidthPt, image.HeightPt);
    }

    static TableSection ToTable(TableBlock table)
    {
        var count = table.Headers.Count;
        count = System.Math.Max(count, table.Rows.Select(static row => row.Count).DefaultIfEmpty().Max());
        var columns = Enumerable.Range(0, count)
            .Select(i => new TableColumn(
                new ColumnWidth.Weight(
                    table.ColumnWidths is { Count: > 0 } widths && i < widths.Count
                        ? System.Math.Max(0.01f, widths[i])
                        : 1f),
                table.ColumnAlignments is { Count: > 0 } aligns && i < aligns.Count
                    ? ToAlignment(aligns[i])
                    : HorizontalAlignment.Left))
            .ToArray();
        var rows = table.Rows.Select(row => new TableRow(
            Enumerable.Range(0, count)
                .Select(i => new TableCell(i < row.Count ? row[i] : string.Empty)))).ToArray();
        var header = table.Headers.Count > 0
            ? new TableRow(Enumerable.Range(0, count)
                .Select(i => new TableCell(i < table.Headers.Count ? table.Headers[i] : string.Empty)))
            : null;
        var options = new TableOptions
        {
            Header = header,
            ShowHeader = table.ShowHeader && header is not null,
            RepeatHeader = table.RepeatHeaderOnPageBreak,
            HeaderBackground = table.HeaderBackground,
            RuleStyle = table.RuleStyle,
        };
        return new TableSection(columns, rows, options);
    }

    static ColumnsSection ToColumns(ColumnsBlock columns)
    {
        var result = new List<Column>();
        for (var i = 0; i < columns.Columns.Count; i++)
        {
            var contents = columns.Columns[i]
                .Select(ConvertContent)
                .Where(static content => content is not null)
                .Cast<IContent>()
                .ToArray();
            var width = columns.Fractions is { Count: > 0 } fractions && i < fractions.Count
                ? new ColumnWidth.Weight(System.Math.Max(0.01f, fractions[i]))
                : new ColumnWidth.Weight(1f);
            result.Add(new Column(contents, width));
        }

        return new ColumnsSection(result, columns.GapPt);
    }

    static IContent? ConvertContent(IBlock block) => block switch
    {
        ParagraphBlock paragraph => new Text(paragraph.Text),
        HeadingBlock heading => new Heading(heading.Text, System.Math.Clamp(heading.Level, 1, 4)),
        ImageBlock image => ToImage(image),
        CodeBlock code => new Code(string.Join(
            Environment.NewLine,
            code.ResolveLines().Select(static line => string.Concat(line.Spans.Select(static span => span.Text))))),
        TextBoxBlock box => new Box(box.Lines.Select(static line => (IContent)new Text(line))),
        SceneBreakBlock scene => new Text(scene.Ornament),
        TableBlock table => ToTable(table),
        _ => null,
    };

    static HorizontalAlignment ToAlignment(CellAlign alignment) => alignment switch
    {
        CellAlign.Center => HorizontalAlignment.Center,
        CellAlign.Right => HorizontalAlignment.Right,
        _ => HorizontalAlignment.Left,
    };
}
