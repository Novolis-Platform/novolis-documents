using Novolis.Documents;
using Novolis.Documents.Layout;
using TUnit.Core;

namespace Novolis.Documents.Unit;

/// <summary>Deterministic measurer: ~0.5em width per char, line height from style.</summary>
sealed class FakeTextMeasurer : ITextMeasurer
{
    public float MeasureHeight(string text, float widthPt, TextStyle style)
    {
        var lines = WrapLines(text, widthPt, style);
        return System.Math.Max(style.FontSizePt, lines.Count * style.FontSizePt * style.LineHeight);
    }

    public IReadOnlyList<string> WrapLines(string text, float widthPt, TextStyle style)
    {
        if (string.IsNullOrEmpty(text))
            return [string.Empty];

        var avgChar = style.FontSizePt * 0.5f;
        var charsPerLine = System.Math.Max(1, (int)(widthPt / avgChar));
        var result = new List<string>();
        foreach (var para in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (para.Length == 0)
            {
                result.Add(string.Empty);
                continue;
            }

            for (var i = 0; i < para.Length; i += charsPerLine)
                result.Add(para.Substring(i, System.Math.Min(charsPerLine, para.Length - i)));
        }

        return result.Count == 0 ? [string.Empty] : result;
    }
}
