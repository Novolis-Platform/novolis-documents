namespace Novolis.Documents;

/// <summary>Diagonal text watermark painted behind page content.</summary>
public sealed class Watermark
{
    /// <summary>Watermark text.</summary>
    public required string Text { get; init; }

    /// <summary>Font size in points.</summary>
    public float FontSizePt { get; init; } = 54f;

    /// <summary>Opacity 0–1 (multiplies over <see cref="Color"/>).</summary>
    public float Opacity { get; init; } = 0.12f;

    /// <summary>Ink color (default <see cref="DocumentColor.Red"/>).</summary>
    public DocumentColor Color { get; init; } = DocumentColor.Red;

    /// <summary>Rotation in degrees (negative = counter-clockwise).</summary>
    public float RotationDegrees { get; init; } = -32f;

    /// <summary>Which regions show the watermark.</summary>
    public WatermarkPages Pages { get; init; } = WatermarkPages.All;
}
