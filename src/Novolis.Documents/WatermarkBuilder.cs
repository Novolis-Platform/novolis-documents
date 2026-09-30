namespace Novolis.Documents;

/// <summary>Fluent builder for <see cref="Watermark"/>.</summary>
public sealed class WatermarkBuilder
{
    string _text = "DRAFT";
    float _fontSizePt = 54f;
    float _opacity = 0.12f;
    DocumentColor _color = DocumentColor.Red;
    float _rotation = -32f;
    WatermarkPages _pages = WatermarkPages.All;

    /// <summary>Watermark text.</summary>
    public WatermarkBuilder Text(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        _text = text;
        return this;
    }

    /// <summary>Font size in points.</summary>
    public WatermarkBuilder FontSize(float points)
    {
        _fontSizePt = points;
        return this;
    }

    /// <summary>Opacity 0–1.</summary>
    public WatermarkBuilder Opacity(float opacity)
    {
        _opacity = opacity;
        return this;
    }

    /// <summary>Ink color (prefer named colors such as <see cref="DocumentColor.Red"/>).</summary>
    public WatermarkBuilder Color(DocumentColor color)
    {
        _color = color;
        return this;
    }

    /// <summary>Rotation in degrees.</summary>
    public WatermarkBuilder Rotation(float degrees)
    {
        _rotation = degrees;
        return this;
    }

    /// <summary>Which regions show the watermark.</summary>
    public WatermarkBuilder On(WatermarkPages pages)
    {
        _pages = pages;
        return this;
    }

    /// <summary>Builds the watermark.</summary>
    public Watermark Build() => new()
    {
        Text = _text,
        FontSizePt = _fontSizePt,
        Opacity = _opacity,
        Color = _color,
        RotationDegrees = _rotation,
        Pages = _pages,
    };
}
