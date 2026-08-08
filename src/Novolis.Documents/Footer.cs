namespace Novolis.Documents;

/// <summary>Page footer configuration (typical home for page numbers).</summary>
public sealed class Footer
{
    /// <summary>
    /// Template text. Placeholders: <c>{page}</c>, <c>{pages}</c>, <c>{title}</c>, and other meta tokens.
    /// </summary>
    public string Template { get; init; } = string.Empty;

    /// <summary>Font size in points.</summary>
    public float FontSizePt { get; init; } = 9f;

    /// <summary>Draw on the opening / title page.</summary>
    public bool IncludeFirstPage { get; init; } = true;

    /// <summary>Draw on TOC pages.</summary>
    public bool IncludeToc { get; init; } = true;

    /// <summary>Draw on body pages.</summary>
    public bool IncludeBody { get; init; } = true;

    /// <summary>Draw on the closing page.</summary>
    public bool IncludeLastPage { get; init; } = true;
}
