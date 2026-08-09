using Novolis.Documents.Layout;
using SkiaSharp;

namespace Novolis.Documents.Skia;

/// <summary>Skia-backed <see cref="ITextMeasurer"/>.</summary>
public sealed class SkiaTextMeasurer : ITextMeasurer
{
    readonly SKTypeface _regular;
    readonly SKTypeface _bold;
    readonly SKTypeface _mono;
    readonly string _monoFamily;

    /// <summary>Creates a measurer with the given typefaces (ownership retained by caller).</summary>
    public SkiaTextMeasurer(SKTypeface regular, SKTypeface bold, SKTypeface? mono = null, string? monoFamily = null)
    {
        _regular = regular ?? throw new ArgumentNullException(nameof(regular));
        _bold = bold ?? throw new ArgumentNullException(nameof(bold));
        _mono = mono ?? regular;
        _monoFamily = monoFamily ?? string.Empty;
    }

    /// <inheritdoc />
    public float MeasureHeight(string text, float widthPt, TextStyle style)
    {
        if (string.IsNullOrEmpty(text))
            return style.FontSizePt * style.LineHeight;

        var lines = WrapLines(text, widthPt, style);
        return System.Math.Max(style.FontSizePt, lines.Count * style.FontSizePt * style.LineHeight);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> WrapLines(string text, float widthPt, TextStyle style)
    {
        var typeface = ResolveTypeface(style);
        using var font = new SKFont(typeface, style.FontSizePt);
        return DocumentPdf.WrapLines(text ?? string.Empty, font, widthPt);
    }

    SKTypeface ResolveTypeface(TextStyle style)
    {
        if (style.Bold)
            return _bold;
        if (!string.IsNullOrEmpty(_monoFamily)
            && style.FontFamily.Equals(_monoFamily, StringComparison.OrdinalIgnoreCase))
            return _mono;
        return _regular;
    }
}
