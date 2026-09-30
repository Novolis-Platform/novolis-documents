namespace Novolis.Documents;

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
