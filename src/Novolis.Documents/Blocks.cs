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

/// <summary>Heading levels 1–4. Level 1 forces a page break when prior content exists.</summary>
public sealed class HeadingBlock : IBlock
{
    /// <summary>1 = top-level section, 2–4 = nested headings.</summary>
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

/// <summary>
/// Bordered text panel (plain lines). Domain-agnostic frame for notes, datelines, callouts, etc.
/// Layout may split across pages by line when content overflows.
/// </summary>
public sealed class TextBoxBlock : IBlock
{
    /// <summary>Lines drawn top-to-bottom inside the box.</summary>
    public IReadOnlyList<string> Lines { get; init; } = [];

    /// <summary>Inner padding in points.</summary>
    public float PaddingPt { get; init; } = 6f;

    /// <summary>Border stroke width in points (0 = no border).</summary>
    public float BorderStrokePt { get; init; } = 0.8f;

    /// <summary>Border ink.</summary>
    public DocumentColor BorderColor { get; init; } = DocumentColor.Gray;

    /// <summary>Optional fill behind the text (null = none).</summary>
    public DocumentColor? Background { get; init; } = DocumentColor.LightGray;

    /// <summary>Optional left accent bar width in points (0 = none).</summary>
    public float AccentBorderLeftPt { get; init; }

    /// <summary>Left accent bar color (used when <see cref="AccentBorderLeftPt"/> &gt; 0).</summary>
    public DocumentColor AccentColor { get; init; } = DocumentColor.Gray;

    /// <summary>Font size in points.</summary>
    public float FontSizePt { get; init; } = 8.5f;

    /// <summary>Line height multiplier.</summary>
    public float LineHeight { get; init; } = 1.22f;

    /// <summary>Extra gap between lines inside the box (points).</summary>
    public float LineGapPt { get; init; } = 1.5f;

    /// <summary>Ink color for lines.</summary>
    public DocumentColor TextColor { get; init; } = DocumentColor.Gray;

    /// <summary>When true, layout/paint use <see cref="Typography.CodeFontFamily"/>.</summary>
    public bool UseMonospaceFont { get; init; }
}

/// <summary>One colored run inside a code line (syntax highlighting).</summary>
/// <param name="Text">Literal text (may be empty).</param>
/// <param name="Color">Ink; null uses the parent <see cref="CodeBlock.TextColor"/>.</param>
public readonly record struct CodeSpan(string Text, DocumentColor? Color = null);

/// <summary>One source line as ordered spans (plain or highlighted).</summary>
public sealed class CodeLine
{
    /// <summary>Runs left-to-right.</summary>
    public IReadOnlyList<CodeSpan> Spans { get; init; } = [];

    /// <summary>Concatenated span text.</summary>
    public string PlainText
    {
        get
        {
            if (Spans.Count == 0)
                return string.Empty;
            if (Spans.Count == 1)
                return Spans[0].Text;
            return string.Concat(Spans.Select(s => s.Text));
        }
    }

    /// <summary>Builds a single-span line in the default ink.</summary>
    public static CodeLine FromPlain(string text) => new()
    {
        Spans = [new CodeSpan(text ?? string.Empty)],
    };
}

/// <summary>
/// Monospace code panel. Layout may split by line across pages.
/// Optional line-number gutter and per-span colors for syntax highlighting.
/// </summary>
public sealed class CodeBlock : IBlock
{
    /// <summary>Source lines drawn top-to-bottom (used when <see cref="StyledLines"/> is null).</summary>
    public IReadOnlyList<string> Lines { get; init; } = [];

    /// <summary>
    /// When set, paint/layout use these lines (and ignore <see cref="Lines"/> for text).
    /// Prefer this for syntax-highlighted output.
    /// </summary>
    public IReadOnlyList<CodeLine>? StyledLines { get; init; }

    /// <summary>Optional language label (informational; highlighter hint).</summary>
    public string? Language { get; init; }

    /// <summary>When true, draw a right-aligned line-number gutter before code text.</summary>
    public bool ShowLineNumbers { get; init; }

    /// <summary>First line number in this block (continuation slices advance).</summary>
    public int FirstLineNumber { get; init; } = 1;

    /// <summary>Line-number ink.</summary>
    public DocumentColor LineNumberColor { get; init; } = DocumentColor.Gray;

    /// <summary>Inner padding in points.</summary>
    public float PaddingPt { get; init; } = 6f;

    /// <summary>Font size in points.</summary>
    public float FontSizePt { get; init; } = 9f;

    /// <summary>Line height multiplier.</summary>
    public float LineHeight { get; init; } = 1.35f;

    /// <summary>Fill behind the text (null = light gray).</summary>
    public DocumentColor? Background { get; init; } = DocumentColor.LightGray;

    /// <summary>Outer border stroke in points (0 = no full box border).</summary>
    public float BorderStrokePt { get; init; }

    /// <summary>Border ink.</summary>
    public DocumentColor BorderColor { get; init; } = DocumentColor.Gray;

    /// <summary>Optional left accent bar width in points (0 = none).</summary>
    public float AccentBorderLeftPt { get; init; }

    /// <summary>Left accent bar color.</summary>
    public DocumentColor AccentColor { get; init; } = DocumentColor.Gray;

    /// <summary>Default ink for unstyled spans / plain lines.</summary>
    public DocumentColor TextColor { get; init; } = DocumentColor.Black;

    /// <summary>Effective lines for layout/paint.</summary>
    public IReadOnlyList<CodeLine> ResolveLines()
    {
        if (StyledLines is { Count: > 0 } styled)
            return styled;
        if (Lines.Count == 0)
            return [CodeLine.FromPlain(string.Empty)];
        return Lines.Select(CodeLine.FromPlain).ToArray();
    }
}

/// <summary>Centered scene-break ornament.</summary>
public sealed class SceneBreakBlock : IBlock
{
    /// <summary>Glyph or short ornament string.</summary>
    public string Ornament { get; init; } = "***";
}

/// <summary>Forced line break (one blank body line of vertical space).</summary>
public sealed class LineBreakBlock : IBlock;

/// <summary>Explicit page break.</summary>
public sealed class PageBreakBlock : IBlock;

/// <summary>Blank page (optional spacer).</summary>
public sealed class BlankPageBlock : IBlock;
