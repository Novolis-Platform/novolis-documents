namespace Novolis.Documents;

/// <summary>Cover page marker (first page). Prefer <see cref="PagedDocument.First"/> / <see cref="PagedDocument.IncludeCover"/>.</summary>
public sealed class CoverBlock : IBlock
{
    /// <summary>Marker only; title-page content comes from <see cref="DocumentMeta"/> / <see cref="FirstPage"/>.</summary>
    public bool Present { get; init; } = true;
}

/// <summary>Contents-page placeholder; entries are filled by layout from level-1 headings.</summary>
public sealed class TocBlock : IBlock
{
    /// <summary>Optional pre-supplied entries; when empty, layout collects level-1 headings.</summary>
    public IReadOnlyList<TocEntry> Entries { get; init; } = [];
}

/// <summary>One contents line.</summary>
/// <param name="Title">Heading title.</param>
/// <param name="PageNumber">1-based page number within the finished plan (0 until layout fills).</param>
public sealed record TocEntry(string Title, int PageNumber = 0);

/// <summary>Heading levels 1–3. Level 1 forces a page break when prior content exists.</summary>
public sealed class HeadingBlock : IBlock
{
    /// <summary>1 = top-level section, 2 = subsection, 3 = sub-subsection.</summary>
    public required int Level { get; init; }

    /// <summary>Heading text.</summary>
    public required string Text { get; init; }
}

/// <summary>Plain paragraph (inlines already resolved by the consumer).</summary>
public sealed class ParagraphBlock : IBlock
{
    /// <summary>Paragraph text.</summary>
    public required string Text { get; init; }
}

/// <summary>Horizontal alignment for a table column.</summary>
public enum CellAlign
{
    /// <summary>Left-aligned (default).</summary>
    Left = 0,

    /// <summary>Centered.</summary>
    Center = 1,

    /// <summary>Right-aligned (amounts, quantities).</summary>
    Right = 2,
}

/// <summary>How table rules are stroked.</summary>
public enum TableRuleStyle
{
    /// <summary>No strokes.</summary>
    None = 0,

    /// <summary>Full cell grid.</summary>
    Grid = 1,

    /// <summary>Header underline + row separators (invoice-style).</summary>
    Horizontal = 2,
}

/// <summary>Simple grid table (string cells).</summary>
public sealed class TableBlock : IBlock
{
    /// <summary>Header cell texts (optional).</summary>
    public IReadOnlyList<string> Headers { get; init; } = [];

    /// <summary>Body rows; each row is a sequence of cell texts.</summary>
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = [];

    /// <summary>
    /// Optional relative column widths (same count as columns). When null/empty, columns share width equally.
    /// </summary>
    public IReadOnlyList<float>? ColumnWidths { get; init; }

    /// <summary>Optional per-column alignment (same count as columns). Defaults to Left.</summary>
    public IReadOnlyList<CellAlign>? ColumnAlignments { get; init; }

    /// <summary>When true and <see cref="Headers"/> is non-empty, draw a header row.</summary>
    public bool ShowHeader { get; init; } = true;

    /// <summary>When true, stroke cell rules (legacy full grid). Prefer <see cref="RuleStyle"/>.</summary>
    public bool DrawRules
    {
        get => RuleStyle != TableRuleStyle.None;
        init => RuleStyle = value ? TableRuleStyle.Grid : TableRuleStyle.None;
    }

    /// <summary>Rule drawing style. Defaults to grid when <see cref="DrawRules"/> is set true via init.</summary>
    public TableRuleStyle RuleStyle { get; init; } = TableRuleStyle.None;

    /// <summary>When true, fill the header row with a light band.</summary>
    public bool HeaderBackground { get; init; }

    /// <summary>Repeat header row when the table breaks across pages.</summary>
    public bool RepeatHeaderOnPageBreak { get; init; } = true;
}

/// <summary>Raster or SVG image drawn at a fixed point size.</summary>
public sealed class ImageBlock : IBlock
{
    /// <summary>Absolute or relative path to PNG/JPEG/SVG. Ignored when <see cref="Data"/> is set.</summary>
    public string? Path { get; init; }

    /// <summary>Raw image bytes (PNG/JPEG/SVG). Takes precedence over <see cref="Path"/>.</summary>
    public byte[]? Data { get; init; }

    /// <summary>Draw width in points.</summary>
    public required float WidthPt { get; init; }

    /// <summary>Draw height in points.</summary>
    public required float HeightPt { get; init; }
}

/// <summary>Side-by-side columns of blocks (e.g. seller | buyer).</summary>
public sealed class ColumnsBlock : IBlock
{
    /// <summary>Columns; each is a vertical stack of blocks.</summary>
    public required IReadOnlyList<IReadOnlyList<IBlock>> Columns { get; init; }

    /// <summary>Gap between columns in points.</summary>
    public float GapPt { get; init; } = 16f;

    /// <summary>
    /// Optional relative column widths (same count as <see cref="Columns"/>). When null, equal widths.
    /// </summary>
    public IReadOnlyList<float>? Fractions { get; init; }
}

/// <summary>Centered scene-break ornament.</summary>
public sealed class SceneBreakBlock : IBlock
{
    /// <summary>Glyph or short ornament string.</summary>
    public string Ornament { get; init; } = "***";
}

/// <summary>Explicit page break.</summary>
public sealed class PageBreakBlock : IBlock;

/// <summary>Blank page (optional spacer).</summary>
public sealed class BlankPageBlock : IBlock;
