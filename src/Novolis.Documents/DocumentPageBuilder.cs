using Novolis.Math.Measure;

namespace Novolis.Documents;

/// <summary>Fluent page setup: trim, margins, header, and footer.</summary>
public sealed class DocumentPageBuilder
{
    Size _trim;
    Thickness _margin;
    Length _headerBand;
    Length _footerBand;
    RunningChrome? _header;
    RunningChrome? _footer;

    internal DocumentPageBuilder(
        Size trim,
        Thickness margin,
        Length headerBand,
        Length footerBand,
        RunningChrome? header,
        RunningChrome? footer)
    {
        _trim = trim;
        _margin = margin;
        _headerBand = headerBand;
        _footerBand = footerBand;
        _header = header;
        _footer = footer;
    }

    internal Size TrimValue => _trim;
    internal Thickness MarginValue => _margin;
    internal Length HeaderBandValue => _headerBand;
    internal Length FooterBandValue => _footerBand;
    internal RunningChrome? HeaderChrome => _header;
    internal RunningChrome? FooterChrome => _footer;

    /// <summary>Page trim size.</summary>
    public DocumentPageBuilder TrimSize(Size trim)
    {
        _trim = trim;
        return this;
    }

    /// <summary>Content margins.</summary>
    public DocumentPageBuilder Margins(Thickness margin)
    {
        _margin = margin;
        return this;
    }

    /// <summary>ISO A4 with print-oriented margins.</summary>
    public DocumentPageBuilder A4() =>
        TrimSize(TrimPresets.A4).Margins(TrimPresets.DefaultMargin);

    /// <summary>6×9″ trade with print-oriented margins.</summary>
    public DocumentPageBuilder Trade6x9() =>
        TrimSize(TrimPresets.Inch6x9).Margins(TrimPresets.DefaultMargin);

    /// <summary>Header template (<c>{page}</c>, <c>{title}</c>).</summary>
    public DocumentPageBuilder Header(string template, float fontSizePt = 9f)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(template);
        _header = new RunningChrome { Template = template, FontSizePt = fontSizePt };
        return this;
    }

    /// <summary>Footer template.</summary>
    public DocumentPageBuilder Footer(string template, float fontSizePt = 9f)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(template);
        _footer = new RunningChrome { Template = template, FontSizePt = fontSizePt };
        return this;
    }

    /// <summary>Header and footer band heights in points.</summary>
    public DocumentPageBuilder Bands(float headerBandPt, float footerBandPt)
    {
        _headerBand = LengthUnits.FromPoints(headerBandPt);
        _footerBand = LengthUnits.FromPoints(footerBandPt);
        return this;
    }
}
