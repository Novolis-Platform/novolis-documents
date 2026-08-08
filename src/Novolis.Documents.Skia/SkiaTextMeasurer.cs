using Novolis.Documents.Layout;
using SkiaSharp;

namespace Novolis.Documents.Skia;

/// <summary>Skia-backed <see cref="ITextMeasurer"/>.</summary>
public sealed class SkiaTextMeasurer : ITextMeasurer
{
    readonly SKTypeface _regular;
    readonly SKTypeface _bold;

    /// <summary>Creates a measurer with the given typefaces (ownership retained by caller).</summary>
    public SkiaTextMeasurer(SKTypeface regular, SKTypeface bold)
    {
        _regular = regular ?? throw new ArgumentNullException(nameof(regular));
        _bold = bold ?? throw new ArgumentNullException(nameof(bold));
    }

    /// <inheritdoc />
    public float MeasureHeight(string text, float widthPt, TextStyle style)
    {
        if (string.IsNullOrEmpty(text))
            return style.FontSizePt * style.LineHeight;

        var typeface = style.Bold ? _bold : _regular;
        using var font = new SKFont(typeface, style.FontSizePt);
        var lines = DocumentPdf.WrapLines(text, font, widthPt);
        return System.Math.Max(style.FontSizePt, lines.Count * style.FontSizePt * style.LineHeight);
    }
}
