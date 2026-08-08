using Novolis.Math.Measure;

namespace Novolis.Documents;

/// <summary>Fluent page setup: trim, margins, header, and footer.</summary>
public sealed class DocumentPageBuilder
{
    Size _trim;
    Thickness _margin;
    Length _headerBand;
    Length _footerBand;
    Header? _header;
    Footer? _footer;

    internal DocumentPageBuilder(
        Size trim,
        Thickness margin,
        Length headerBand,
        Length footerBand,
        Header? header,
        Footer? footer)
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
    internal Header? HeaderValue => _header;
    internal Footer? FooterValue => _footer;

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

    /// <summary>Header and footer band heights in points.</summary>
    public DocumentPageBuilder Bands(float headerBandPt, float footerBandPt)
    {
        _headerBand = LengthUnits.FromPoints(headerBandPt);
        _footerBand = LengthUnits.FromPoints(footerBandPt);
        return this;
    }

    /// <summary>Configures the page header.</summary>
    public DocumentPageBuilder Header(Action<HeaderBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new HeaderBuilder();
        configure(builder);
        _header = builder.Build();
        return this;
    }

    /// <summary>Simple header template on body pages.</summary>
    public DocumentPageBuilder Header(string template, float fontSizePt = 9f) =>
        Header(h => h.Template(template).FontSize(fontSizePt).IncludeBody());

    /// <summary>Configures the page footer.</summary>
    public DocumentPageBuilder Footer(Action<FooterBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new FooterBuilder();
        configure(builder);
        _footer = builder.Build();
        return this;
    }

    /// <summary>Simple footer template on body pages.</summary>
    public DocumentPageBuilder Footer(string template, float fontSizePt = 9f) =>
        Footer(f => f.Template(template).FontSize(fontSizePt).IncludeBody());
}
