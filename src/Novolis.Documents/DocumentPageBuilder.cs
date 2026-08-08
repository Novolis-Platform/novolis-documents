using Novolis.Math.Measure;

namespace Novolis.Documents;

/// <summary>Fluent page setup: trim, margins, header, footer, and chrome visibility.</summary>
public sealed class DocumentPageBuilder
{
    Size _trim;
    Thickness _margin;
    Length _headerBand;
    Length _footerBand;
    RunningChrome? _header;
    RunningChrome? _footer;
    ChromeOptions _chrome;

    internal DocumentPageBuilder(
        Size trim,
        Thickness margin,
        Length headerBand,
        Length footerBand,
        RunningChrome? header,
        RunningChrome? footer,
        ChromeOptions chrome)
    {
        _trim = trim;
        _margin = margin;
        _headerBand = headerBand;
        _footerBand = footerBand;
        _header = header;
        _footer = footer;
        _chrome = chrome;
    }

    internal Size TrimValue => _trim;
    internal Thickness MarginValue => _margin;
    internal Length HeaderBandValue => _headerBand;
    internal Length FooterBandValue => _footerBand;
    internal RunningChrome? HeaderChrome => _header;
    internal RunningChrome? FooterChrome => _footer;
    internal ChromeOptions ChromeValue => _chrome;

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

    /// <summary>Header template (<c>{page}</c>, <c>{title}</c>, …).</summary>
    public DocumentPageBuilder Header(string template, float fontSizePt = 9f)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(template);
        _header = new RunningChrome { Template = template, FontSizePt = fontSizePt };
        return this;
    }

    /// <summary>Footer template (often <c>{page}</c> or <c>{page} / {pages}</c>).</summary>
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

    /// <summary>Per-region header/footer visibility (First / Toc / Body / Last).</summary>
    public DocumentPageBuilder Chrome(Action<ChromeOptionsBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new ChromeOptionsBuilder();
        configure(builder);
        _chrome = builder.Build();
        return this;
    }

    /// <summary>Replaces chrome options with a finished instance.</summary>
    public DocumentPageBuilder Chrome(ChromeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _chrome = options;
        return this;
    }
}
