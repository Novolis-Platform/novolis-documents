namespace Novolis.Documents;

/// <summary>Typography for one-column paged flow.</summary>
public sealed class Typography
{
    /// <summary>Body font family name (informational; Skia embeds Liberation Serif by default).</summary>
    public string BodyFontFamily { get; init; } = "Liberation Serif";

    /// <summary>Body size in points.</summary>
    public float BodyFontSizePt { get; init; } = 11f;

    /// <summary>Level-1 heading size in points.</summary>
    public float H1SizePt { get; init; } = 18f;

    /// <summary>Level-2 heading size in points.</summary>
    public float H2SizePt { get; init; } = 13.5f;

    /// <summary>Level-3 heading size in points.</summary>
    public float H3SizePt { get; init; } = 12f;

    /// <summary>Scene-break ornament size in points.</summary>
    public float SceneBreakSizePt { get; init; } = 18f;

    /// <summary>Table cell text size in points (defaults to body size when ≤ 0).</summary>
    public float TableFontSizePt { get; init; } = 0f;

    /// <summary>Line height multiplier for body text.</summary>
    public float LineHeight { get; init; } = 1.28f;

    /// <summary>Spacing between block items in points.</summary>
    public float ParagraphSpacingPt { get; init; } = 6f;

    /// <summary>Extra space after level-1 headings in points.</summary>
    public float AfterLevel1SpacingPt { get; init; } = 10f;

    /// <summary>Extra space after level-2/3 headings in points.</summary>
    public float AfterHeadingSpacingPt { get; init; } = 6f;

    /// <summary>Horizontal padding inside table cells in points.</summary>
    public float TableCellPaddingPt { get; init; } = 4f;

    /// <summary>Table rule stroke width in points.</summary>
    public float TableRuleStrokePt { get; init; } = 0.5f;

    /// <summary>Effective table font size.</summary>
    public float EffectiveTableFontSizePt => TableFontSizePt > 0 ? TableFontSizePt : BodyFontSizePt;
}
