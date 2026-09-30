namespace Novolis.Documents;

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
