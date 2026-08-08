namespace Novolis.Documents;

/// <summary>Which page regions receive a watermark.</summary>
[Flags]
public enum WatermarkPages
{
    /// <summary>No pages.</summary>
    None = 0,

    /// <summary>Opening / title page.</summary>
    First = 1,

    /// <summary>Table-of-contents pages.</summary>
    Toc = 2,

    /// <summary>Main content pages.</summary>
    Body = 4,

    /// <summary>Closing page.</summary>
    Last = 8,

    /// <summary>Every page.</summary>
    All = First | Toc | Body | Last,
}

/// <summary>Diagonal text watermark painted behind page content.</summary>
public sealed class Watermark
{
    /// <summary>Watermark text.</summary>
    public required string Text { get; init; }

    /// <summary>Font size in points.</summary>
    public float FontSizePt { get; init; } = 54f;

    /// <summary>Opacity 0–1 (multiplies over <see cref="Color"/>).</summary>
    public float Opacity { get; init; } = 0.12f;

    /// <summary>Ink color (default gray).</summary>
    public DocumentColor Color { get; init; } = DocumentColor.Gray;

    /// <summary>Rotation in degrees (negative = counter-clockwise).</summary>
    public float RotationDegrees { get; init; } = -32f;

    /// <summary>Which regions show the watermark.</summary>
    public WatermarkPages Pages { get; init; } = WatermarkPages.All;
}
