using Novolis.Documents;

namespace Novolis.Documents.Layout;

/// <summary>Typed layout failure categories.</summary>
public enum LayoutFailureKind
{
    /// <summary>A section cannot fit the usable page area.</summary>
    SectionTooLarge,

    /// <summary>A table row cannot fit an empty page.</summary>
    TableRowTooLarge,

    /// <summary>The page has no usable content area.</summary>
    NoUsablePageArea,

    /// <summary>A width configuration cannot be resolved.</summary>
    InvalidColumnConfiguration,

    /// <summary>A keep-together request cannot be satisfied.</summary>
    UnsatisfiableKeepTogether,

    /// <summary>The content type has no layout implementation.</summary>
    UnsupportedContent,
}

/// <summary>Structured diagnostic raised by section pagination.</summary>
public sealed class LayoutFailure : Exception
{
    /// <summary>Creates a diagnostic.</summary>
    public LayoutFailure(
        LayoutFailureKind kind,
        string message,
        Type? sectionType = null,
        float availableWidthPt = 0f,
        float availableHeightPt = 0f,
        float measuredHeightPt = 0f,
        int? rowIndex = null)
        : base(message)
    {
        Kind = kind;
        SectionType = sectionType;
        AvailableWidthPt = availableWidthPt;
        AvailableHeightPt = availableHeightPt;
        MeasuredHeightPt = measuredHeightPt;
        RowIndex = rowIndex;
    }

    /// <summary>Failure category.</summary>
    public LayoutFailureKind Kind { get; }

    /// <summary>Section type being laid out.</summary>
    public Type? SectionType { get; }

    /// <summary>Width available at the failure point.</summary>
    public float AvailableWidthPt { get; }

    /// <summary>Height available at the failure point.</summary>
    public float AvailableHeightPt { get; }

    /// <summary>Measured height of the failing item.</summary>
    public float MeasuredHeightPt { get; }

    /// <summary>Table row index when the failure concerns a row.</summary>
    public int? RowIndex { get; }
}

/// <summary>Renderer-independent semantic fragment produced by section layout.</summary>
public abstract record SectionFragment(ISection Source, float HeightPt);

/// <summary>Fragment of a normal vertical section.</summary>
public sealed record FlowFragment(
    ISection Source,
    IReadOnlyList<IContent> Contents,
    float HeightPt) : SectionFragment(Source, HeightPt);

/// <summary>Fragment of a columns section.</summary>
public sealed record ColumnsFragment : SectionFragment
{
    /// <summary>Creates a columns fragment with resolved widths.</summary>
    public ColumnsFragment(
        ISection source,
        float heightPt,
        IReadOnlyList<float>? columnWidths = null)
        : base(source, heightPt)
    {
        ColumnWidths = columnWidths ?? [];
    }

    /// <summary>Resolved column widths in points.</summary>
    public IReadOnlyList<float> ColumnWidths { get; }
}

/// <summary>Fragment of a table section.</summary>
public sealed record TableFragment : SectionFragment
{
    /// <summary>Creates a table fragment.</summary>
    public TableFragment(
        TableSection source,
        IReadOnlyList<TableRow> rows,
        TableRow? header,
        float heightPt,
        bool isContinuation,
        bool hasRepeatedHeader,
        IReadOnlyList<float>? columnWidths = null,
        TableRow? footer = null)
        : base(source, heightPt)
    {
        Rows = rows;
        Header = header;
        IsContinuation = isContinuation;
        HasRepeatedHeader = hasRepeatedHeader;
        ColumnWidths = columnWidths ?? [];
        Footer = footer;
    }

    /// <summary>Source table.</summary>
    public TableSection Table => (TableSection)Source;

    /// <summary>Body rows on this page.</summary>
    public IReadOnlyList<TableRow> Rows { get; }

    /// <summary>Header painted on this fragment.</summary>
    public TableRow? Header { get; }

    /// <summary>Whether this is a continuation fragment.</summary>
    public bool IsContinuation { get; }

    /// <summary>Whether the header is repeated rather than the initial header.</summary>
    public bool HasRepeatedHeader { get; }

    /// <summary>Resolved column widths in points.</summary>
    public IReadOnlyList<float> ColumnWidths { get; }

    /// <summary>Final summary row painted after the last body fragment.</summary>
    public TableRow? Footer { get; }
}

/// <summary>Fragment of a boxed content section.</summary>
public sealed record BoxFragment(
    ISection Source,
    float HeightPt) : SectionFragment(Source, HeightPt);

/// <summary>Positioned semantic fragment on a page.</summary>
public sealed record PlacedFragment(
    SectionFragment Fragment,
    float YInContentPt,
    float HeightPt)
{
    /// <summary>Source section.</summary>
    public ISection Section => Fragment.Source;
}

/// <summary>Explicit result of attempting to lay out a section.</summary>
public abstract record SectionLayoutResult
{
    /// <summary>The complete section fragment fits.</summary>
    public sealed record Fits(SectionFragment Fragment) : SectionLayoutResult;

    /// <summary>A head fits and the returned tail continues later.</summary>
    public sealed record Split(SectionFragment Head, ISection Tail) : SectionLayoutResult;

    /// <summary>The section should be retried on a new page.</summary>
    public sealed record NeedsNewPage : SectionLayoutResult;

    /// <summary>The section cannot be represented in the available geometry.</summary>
    public sealed record Impossible(LayoutFailure Failure) : SectionLayoutResult;
}
