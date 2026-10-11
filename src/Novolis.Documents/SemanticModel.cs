using System.Collections.ObjectModel;

namespace Novolis.Documents;

/// <summary>Participates directly in document flow and pagination.</summary>
public interface ISection;

/// <summary>May be placed inside a section or container.</summary>
public interface IContent;

/// <summary>Owns nested content in a local vertical flow.</summary>
public interface IContainer : IContent
{
    /// <summary>Nested content in reading order.</summary>
    IReadOnlyList<IContent> Contents { get; }
}

/// <summary>Common section-level pagination policy.</summary>
public sealed record SectionLayout
{
    /// <summary>When true, move the complete section when it fits on an empty page.</summary>
    public bool KeepTogether { get; init; }

    /// <summary>When true, reserve room for the next section's minimum fragment.</summary>
    public bool KeepWithNext { get; init; }

    /// <summary>Space before the section in points.</summary>
    public float SpaceBefore { get; init; }

    /// <summary>Space after the section in points.</summary>
    public float SpaceAfter { get; init; }

    /// <summary>Default section policy.</summary>
    public static SectionLayout Default { get; } = new();
}

/// <summary>A normal vertically flowing section.</summary>
public sealed class Section : ISection, IContainer
{
    /// <summary>Creates a section from semantic content.</summary>
    public Section(IEnumerable<IContent> contents, SectionLayout? layout = null)
    {
        ArgumentNullException.ThrowIfNull(contents);
        Contents = Copy(contents);
        Layout = layout ?? SectionLayout.Default;
    }

    /// <summary>Creates an empty section.</summary>
    public Section(SectionLayout? layout = null)
        : this(Array.Empty<IContent>(), layout)
    {
    }

    /// <inheritdoc />
    public IReadOnlyList<IContent> Contents { get; }

    /// <summary>Pagination policy.</summary>
    public SectionLayout Layout { get; }

    static IReadOnlyList<T> Copy<T>(IEnumerable<T> values) =>
        new ReadOnlyCollection<T>(values.ToArray());
}

/// <summary>Text content that remains semantic until layout and rendering.</summary>
public sealed record Text(string Value, ContentStyle? Style = null) : IContent
{
    /// <summary>Creates text while accepting null as an empty value.</summary>
    public static Text Empty { get; } = new(string.Empty);
}

/// <summary>Optional presentation details for text content.</summary>
public sealed record ContentStyle
{
    /// <summary>Optional font family hint.</summary>
    public string? FontFamily { get; init; }

    /// <summary>Optional font size in points.</summary>
    public float? FontSizePt { get; init; }

    /// <summary>Whether the text uses a bold face.</summary>
    public bool Bold { get; init; }

    /// <summary>Text ink.</summary>
    public DocumentColor Color { get; init; } = DocumentColor.Black;
}

/// <summary>Number presentation styles retained until rendering.</summary>
public enum NumberStyle
{
    /// <summary>Locale-aware decimal value.</summary>
    Decimal,

    /// <summary>Whole-number presentation.</summary>
    Integer,

    /// <summary>Quantity presentation.</summary>
    Quantity,

    /// <summary>Percentage presentation.</summary>
    Percentage,

    /// <summary>Currency presentation.</summary>
    Currency,
}

/// <summary>Formatting metadata for a semantic number.</summary>
public sealed record NumberFormat
{
    /// <summary>Number presentation style.</summary>
    public NumberStyle Style { get; init; } = NumberStyle.Decimal;

    /// <summary>ISO currency code for currency values.</summary>
    public string? CurrencyCode { get; init; }

    /// <summary>Optional fixed decimal precision.</summary>
    public int? DecimalPlaces { get; init; }

    /// <summary>Creates a decimal format.</summary>
    public static NumberFormat Decimal(int? decimalPlaces = null) =>
        new() { Style = NumberStyle.Decimal, DecimalPlaces = decimalPlaces };

    /// <summary>Creates an integer format.</summary>
    public static NumberFormat Integer() => new() { Style = NumberStyle.Integer, DecimalPlaces = 0 };

    /// <summary>Creates a quantity format.</summary>
    public static NumberFormat Quantity(int? decimalPlaces = null) =>
        new() { Style = NumberStyle.Quantity, DecimalPlaces = decimalPlaces };

    /// <summary>Creates a percentage format.</summary>
    public static NumberFormat Percentage(int? decimalPlaces = null) =>
        new() { Style = NumberStyle.Percentage, DecimalPlaces = decimalPlaces };

    /// <summary>Creates a currency format.</summary>
    public static NumberFormat Currency(string currencyCode, int? decimalPlaces = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        return new()
        {
            Style = NumberStyle.Currency,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            DecimalPlaces = decimalPlaces,
        };
    }
}

/// <summary>Numeric content with formatting retained separately from its value.</summary>
public sealed record Number(decimal Value, NumberFormat Format) : IContent
{
    /// <summary>Creates a decimal number.</summary>
    public Number(decimal value)
        : this(value, NumberFormat.Decimal())
    {
    }
}

/// <summary>Hierarchical heading content.</summary>
public sealed record Heading(string Value, int Level = 1, ContentStyle? Style = null) : IContent;

/// <summary>Image source independent of a renderer.</summary>
public abstract record ImageSource
{
    /// <summary>Creates a file-backed image source.</summary>
    public static ImageSource FromPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return new PathImageSource(path);
    }

    /// <summary>Creates a byte-backed image source.</summary>
    public static ImageSource FromBytes(byte[] data, bool svg = false)
    {
        ArgumentNullException.ThrowIfNull(data);
        return svg ? new SvgImageSource(data) : new RasterImageSource(data);
    }
}

/// <summary>File-backed image source.</summary>
public sealed record PathImageSource(string Path) : ImageSource
{
    /// <summary>Validates the path.</summary>
    public PathImageSource(string path, bool validate = true)
        : this(path)
    {
        if (validate)
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
    }
}

/// <summary>Raster image bytes.</summary>
public sealed record RasterImageSource(byte[] Data) : ImageSource
{
    /// <summary>Copies the supplied bytes.</summary>
    public RasterImageSource(byte[] data, bool copy = true)
        : this(copy ? data.ToArray() : data)
    {
        ArgumentNullException.ThrowIfNull(data);
    }
}

/// <summary>SVG image bytes.</summary>
public sealed record SvgImageSource(byte[] Data) : ImageSource
{
    /// <summary>Copies the supplied bytes.</summary>
    public SvgImageSource(byte[] data, bool copy = true)
        : this(copy ? data.ToArray() : data)
    {
        ArgumentNullException.ThrowIfNull(data);
    }
}

/// <summary>Requested image size in points.</summary>
public sealed record ImageSizing(float WidthPt, float HeightPt)
{
    /// <summary>Validates positive dimensions.</summary>
    public ImageSizing(float widthPt, float heightPt, bool validate = true)
        : this(widthPt, heightPt)
    {
        if (validate)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(widthPt);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(heightPt);
        }
    }
}

/// <summary>Image content with renderer-neutral source and fixed dimensions.</summary>
public sealed record Image(ImageSource Source, ImageSizing Sizing) : IContent
{
    /// <summary>Creates a file-backed image.</summary>
    public Image(string path, float widthPt, float heightPt)
        : this(ImageSource.FromPath(path), new ImageSizing(widthPt, heightPt))
    {
    }

    /// <summary>Creates a byte-backed image.</summary>
    public Image(byte[] data, float widthPt, float heightPt, bool svg = false)
        : this(ImageSource.FromBytes(data, svg), new ImageSizing(widthPt, heightPt))
    {
    }
}

/// <summary>Horizontal alignment for columns and table cells.</summary>
public enum HorizontalAlignment
{
    /// <summary>Align to the leading edge.</summary>
    Left,

    /// <summary>Center within the available width.</summary>
    Center,

    /// <summary>Align to the trailing edge.</summary>
    Right,
}

/// <summary>Column width resolution policy.</summary>
public abstract record ColumnWidth
{
    /// <summary>Fixed width in points.</summary>
    public sealed record Fixed(float Points) : ColumnWidth;

    /// <summary>Width measured from the column's content.</summary>
    public sealed record Auto : ColumnWidth;

    /// <summary>Share of remaining width.</summary>
    public sealed record Weight(float Value) : ColumnWidth;

    /// <summary>Creates a fixed-width policy.</summary>
    public static ColumnWidth FixedPoints(float points) => new Fixed(points);

    /// <summary>Creates an automatic-width policy.</summary>
    public static ColumnWidth AutoSize() => new Auto();

    /// <summary>Creates a weighted-width policy.</summary>
    public static ColumnWidth WeightBy(float value = 1f) => new Weight(value);
}

/// <summary>One column inside a columns section.</summary>
public sealed class Column : IContainer
{
    /// <summary>Creates a column.</summary>
    public Column(IEnumerable<IContent> contents, ColumnWidth? width = null)
    {
        ArgumentNullException.ThrowIfNull(contents);
        Contents = contents.ToArray();
        Width = width ?? new ColumnWidth.Weight(1f);
    }

    /// <summary>Creates an empty weighted column.</summary>
    public Column(ColumnWidth? width = null)
        : this(Array.Empty<IContent>(), width)
    {
    }

    /// <inheritdoc />
    public IReadOnlyList<IContent> Contents { get; }

    /// <summary>Width policy.</summary>
    public ColumnWidth Width { get; }
}

/// <summary>Horizontal composition contained in one document-flow section.</summary>
public sealed class ColumnsSection : ISection, IContent
{
    /// <summary>Creates a columns section.</summary>
    public ColumnsSection(
        IEnumerable<Column> columns,
        float gapPt = 12f,
        SectionLayout? layout = null)
    {
        ArgumentNullException.ThrowIfNull(columns);
        Columns = columns.ToArray();
        GapPt = gapPt;
        Layout = layout ?? new SectionLayout { KeepTogether = true };
    }

    /// <summary>Columns in left-to-right order.</summary>
    public IReadOnlyList<Column> Columns { get; }

    /// <summary>Gap between columns in points.</summary>
    public float GapPt { get; }

    /// <summary>Pagination policy.</summary>
    public SectionLayout Layout { get; }
}

/// <summary>A table column.</summary>
public sealed record TableColumn(
    ColumnWidth Width,
    HorizontalAlignment Alignment = HorizontalAlignment.Left);

/// <summary>A table cell containing local-flow content.</summary>
public sealed class TableCell : IContainer
{
    /// <summary>Creates a cell.</summary>
    public TableCell(IEnumerable<IContent> contents)
    {
        ArgumentNullException.ThrowIfNull(contents);
        Contents = contents.ToArray();
    }

    /// <summary>Creates a text cell.</summary>
    public TableCell(string value)
        : this([new Text(value)])
    {
    }

    /// <summary>Creates a single-content cell.</summary>
    public TableCell(IContent content)
        : this([content])
    {
    }

    /// <inheritdoc />
    public IReadOnlyList<IContent> Contents { get; }
}

/// <summary>A non-fragmentable table row.</summary>
public sealed class TableRow
{
    /// <summary>Creates a row.</summary>
    public TableRow(IEnumerable<TableCell> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        Cells = cells.ToArray();
    }

    /// <summary>Creates a row from text cells.</summary>
    public TableRow(params string[] cells)
        : this(cells.Select(static cell => new TableCell(cell)))
    {
    }

    /// <summary>Cells in column order.</summary>
    public IReadOnlyList<TableCell> Cells { get; }
}

/// <summary>Semantic table styling and pagination options.</summary>
public sealed record TableOptions
{
    /// <summary>Whether a header row is painted.</summary>
    public bool ShowHeader { get; init; } = true;

    /// <summary>Whether a header is repeated on continuation fragments.</summary>
    public bool RepeatHeader { get; init; } = true;

    /// <summary>Header background fill.</summary>
    public bool HeaderBackground { get; init; } = true;

    /// <summary>Cell padding in points.</summary>
    public float CellPaddingPt { get; init; } = 4f;

    /// <summary>Rule style.</summary>
    public TableRuleStyle RuleStyle { get; init; } = TableRuleStyle.Horizontal;

    /// <summary>Header row.</summary>
    public TableRow? Header { get; init; }

    /// <summary>Optional final summary row.</summary>
    public TableRow? Footer { get; init; }
}

/// <summary>First-class table section.</summary>
public sealed class TableSection : ISection, IContent
{
    /// <summary>Creates a table section.</summary>
    public TableSection(
        IEnumerable<TableColumn> columns,
        IEnumerable<TableRow> rows,
        TableOptions? options = null,
        SectionLayout? layout = null)
    {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);
        Columns = columns.ToArray();
        Rows = rows.ToArray();
        Options = options ?? new TableOptions();
        Layout = layout ?? SectionLayout.Default;
    }

    /// <summary>Creates a table from rows and inferred columns.</summary>
    public TableSection(
        IEnumerable<TableRow> rows,
        TableOptions? options = null,
        SectionLayout? layout = null)
        : this(InferColumns(rows), rows, options, layout)
    {
    }

    /// <summary>Column definitions.</summary>
    public IReadOnlyList<TableColumn> Columns { get; }

    /// <summary>Rows in source order.</summary>
    public IReadOnlyList<TableRow> Rows { get; }

    /// <summary>Table styling and pagination options.</summary>
    public TableOptions Options { get; }

    /// <summary>Pagination policy.</summary>
    public SectionLayout Layout { get; }

    static IReadOnlyList<TableColumn> InferColumns(IEnumerable<TableRow> rows)
    {
        var materialized = rows.ToArray();
        var count = materialized.Select(static row => row.Cells.Count).DefaultIfEmpty().Max();
        return Enumerable.Range(0, count)
            .Select(static _ => new TableColumn(new ColumnWidth.Weight(1f)))
            .ToArray();
    }
}

/// <summary>Explicit structural page break.</summary>
public sealed record PageBreakSection : ISection;

/// <summary>Rule content, usually used for scene breaks.</summary>
public sealed record Rule(DocumentColor Color, float StrokeWidthPt = 0.6f) : IContent
{
    /// <summary>Creates a neutral rule.</summary>
    public Rule()
        : this(DocumentColor.Gray)
    {
    }
}

/// <summary>Vertical space content.</summary>
public sealed record Spacer(float HeightPt) : IContent
{
    /// <summary>Validates a non-negative height.</summary>
    public Spacer(float heightPt, bool validate = true)
        : this(heightPt)
    {
        if (validate)
            ArgumentOutOfRangeException.ThrowIfNegative(heightPt);
    }
}

/// <summary>Key/value content for compact summaries.</summary>
public sealed record KeyValue(string Key, IContent Value) : IContent
{
    /// <summary>Creates a textual key/value pair.</summary>
    public KeyValue(string key, string value)
        : this(key, new Text(value))
    {
    }

    /// <summary>Creates a numeric key/value pair.</summary>
    public KeyValue(string key, Number value)
        : this(key, (IContent)value)
    {
    }
}

/// <summary>Monospace-oriented code content retained for document consumers.</summary>
public sealed record Code(string Source, string? Language = null) : IContent;

/// <summary>Simple bordered content container.</summary>
public sealed class Box : ISection, IContainer
{
    /// <summary>Creates a boxed section.</summary>
    public Box(IEnumerable<IContent> contents, SectionLayout? layout = null)
    {
        ArgumentNullException.ThrowIfNull(contents);
        Contents = contents.ToArray();
        Layout = layout ?? SectionLayout.Default;
    }

    /// <inheritdoc />
    public IReadOnlyList<IContent> Contents { get; }

    /// <summary>Pagination policy.</summary>
    public SectionLayout Layout { get; }

    /// <summary>Box fill.</summary>
    public DocumentColor? Background { get; init; } = DocumentColor.LightGray;

    /// <summary>Box border.</summary>
    public DocumentColor BorderColor { get; init; } = DocumentColor.Gray;
}

/// <summary>Sections assigned to a unique first or last page role.</summary>
public sealed class PageDefinition
{
    /// <summary>Creates a page definition.</summary>
    public PageDefinition(IEnumerable<ISection> sections)
    {
        ArgumentNullException.ThrowIfNull(sections);
        Sections = sections.ToArray();
    }

    /// <summary>Creates an empty page definition.</summary>
    public PageDefinition()
        : this(Array.Empty<ISection>())
    {
    }

    /// <summary>Sections in page order.</summary>
    public IReadOnlyList<ISection> Sections { get; }
}

/// <summary>Document-wide style defaults.</summary>
public sealed record DocumentTheme
{
    /// <summary>Typography defaults.</summary>
    public Typography Typography { get; init; } = new();

    /// <summary>Body ink.</summary>
    public DocumentColor TextColor { get; init; } = DocumentColor.Black;

    /// <summary>Table header fill.</summary>
    public DocumentColor TableHeaderBackground { get; init; } = DocumentColor.LightGray;

    /// <summary>Table rule ink.</summary>
    public DocumentColor TableRuleColor { get; init; } = DocumentColor.Gray;

    /// <summary>Default theme.</summary>
    public static DocumentTheme Default { get; } = new();
}
