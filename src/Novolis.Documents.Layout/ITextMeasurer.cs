using Novolis.Documents;

namespace Novolis.Documents.Layout;

/// <summary>Measures text height for a given width and style (Skia or test fake).</summary>
public interface ITextMeasurer
{
    /// <summary>Returns the height in points required to draw <paramref name="text"/>.</summary>
    float MeasureHeight(string text, float widthPt, TextStyle style);

    /// <summary>Wraps <paramref name="text"/> into display lines for <paramref name="widthPt"/>.</summary>
    IReadOnlyList<string> WrapLines(string text, float widthPt, TextStyle style);
}
