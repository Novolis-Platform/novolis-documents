namespace Novolis.Documents;

/// <summary>Which running bands to draw when header/footer templates are set.</summary>
[Flags]
public enum ChromeBand
{
    /// <summary>No header or footer.</summary>
    None = 0,

    /// <summary>Running header band.</summary>
    Header = 1,

    /// <summary>Running footer band (typical place for page numbers).</summary>
    Footer = 2,

    /// <summary>Both header and footer.</summary>
    HeaderAndFooter = Header | Footer,
}

/// <summary>Per-region header/footer visibility (First, Toc, Body, Last).</summary>
public sealed class ChromeOptions
{
    /// <summary>
    /// Defaults: footer (page numbers) on First / Toc / Last; header+footer on Body.
    /// </summary>
    public static ChromeOptions Default { get; } = new();

    /// <summary>Opening / title page.</summary>
    public ChromeBand First { get; init; } = ChromeBand.Footer;

    /// <summary>Table-of-contents pages.</summary>
    public ChromeBand Toc { get; init; } = ChromeBand.Footer;

    /// <summary>Main content pages.</summary>
    public ChromeBand Body { get; init; } = ChromeBand.HeaderAndFooter;

    /// <summary>Closing page.</summary>
    public ChromeBand Last { get; init; } = ChromeBand.Footer;

    /// <summary>Whether <paramref name="band"/> includes a header.</summary>
    public static bool HasHeader(ChromeBand band) => (band & ChromeBand.Header) != 0;

    /// <summary>Whether <paramref name="band"/> includes a footer.</summary>
    public static bool HasFooter(ChromeBand band) => (band & ChromeBand.Footer) != 0;
}
