namespace Novolis.Documents;

/// <summary>Fluent builder for <see cref="TextBoxBlock"/>.</summary>
public sealed class TextBoxBuilder
{
    readonly List<string> _lines = [];
    float _paddingPt = 6f;
    float _borderStrokePt = 0.8f;
    DocumentColor _borderColor = DocumentColor.Gray;
    DocumentColor? _background = DocumentColor.LightGray;
    float _fontSizePt = 8.5f;
    float _lineHeight = 1.22f;
    float _lineGapPt = 1.5f;
    DocumentColor _textColor = DocumentColor.Gray;

    /// <summary>Replaces the box lines.</summary>
    public TextBoxBuilder Lines(params string[] lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        _lines.Clear();
        _lines.AddRange(lines);
        return this;
    }

    /// <summary>Appends lines.</summary>
    public TextBoxBuilder AddLines(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        _lines.AddRange(lines);
        return this;
    }

    /// <summary>Inner padding in points.</summary>
    public TextBoxBuilder Padding(float points)
    {
        _paddingPt = points;
        return this;
    }

    /// <summary>Border stroke in points (0 disables).</summary>
    public TextBoxBuilder Border(float strokePt, DocumentColor? color = null)
    {
        _borderStrokePt = strokePt;
        if (color is { } c)
            _borderColor = c;
        return this;
    }

    /// <summary>Background fill (null clears).</summary>
    public TextBoxBuilder Background(DocumentColor? color)
    {
        _background = color;
        return this;
    }

    /// <summary>Type size and line metrics.</summary>
    public TextBoxBuilder Font(float sizePt, float lineHeight = 1.22f, float lineGapPt = 1.5f)
    {
        _fontSizePt = sizePt;
        _lineHeight = lineHeight;
        _lineGapPt = lineGapPt;
        return this;
    }

    /// <summary>Text ink.</summary>
    public TextBoxBuilder TextColor(DocumentColor color)
    {
        _textColor = color;
        return this;
    }

    /// <summary>Builds an immutable <see cref="TextBoxBlock"/>.</summary>
    public TextBoxBlock Build() => new()
    {
        Lines = _lines.ToArray(),
        PaddingPt = _paddingPt,
        BorderStrokePt = _borderStrokePt,
        BorderColor = _borderColor,
        Background = _background,
        FontSizePt = _fontSizePt,
        LineHeight = _lineHeight,
        LineGapPt = _lineGapPt,
        TextColor = _textColor,
    };
}
