namespace Novolis.Documents;

/// <summary>Minimal typography for one-column paged flow.</summary>
public sealed class Typography
{
    /// <summary>Body font family name (system or embedded via Skia options).</summary>
    public string BodyFontFamily { get; init; } = "Georgia";

    /// <summary>Body size in points.</summary>
    public float BodyFontSizePt { get; init; } = 11f;

    /// <summary>H1 size in points.</summary>
    public float H1SizePt { get; init; } = 19f;

    /// <summary>H2 size in points.</summary>
    public float H2SizePt { get; init; } = 14f;

    /// <summary>H3 size in points.</summary>
    public float H3SizePt { get; init; } = 12f;

    /// <summary>Scene-break ornament size in points.</summary>
    public float SceneBreakSizePt { get; init; } = 22f;

    /// <summary>Line height multiplier for body text.</summary>
    public float LineHeight { get; init; } = 1.42f;

    /// <summary>Spacing between block items in points.</summary>
    public float ParagraphSpacingPt { get; init; } = 8f;
}
