namespace Novolis.Documents;

/// <summary>Page header configuration.</summary>
public sealed class Header
{
    /// <summary>
    /// Template text. Placeholders: <c>{page}</c>, <c>{pages}</c>, <c>{title}</c>, <c>{subtitle}</c>,
    /// <c>{author}</c>, <c>{series}</c>, <c>{publisher}</c>, <c>{subject}</c>, <c>{identifier}</c>,
    /// <c>{version}</c>, <c>{language}</c>, <c>{date}</c>, <c>{rights}</c>, <c>{chapter}</c>.
    /// </summary>
    public string Template { get; init; } = string.Empty;

    /// <summary>Font size in points.</summary>
    public float FontSizePt { get; init; } = 9f;

    /// <summary>Draw on the opening / title page.</summary>
    public bool IncludeFirstPage { get; init; }

    /// <summary>Draw on TOC pages.</summary>
    public bool IncludeToc { get; init; }

    /// <summary>Draw on body pages.</summary>
    public bool IncludeBody { get; init; } = true;

    /// <summary>Draw on the closing page.</summary>
    public bool IncludeLastPage { get; init; }

    /// <summary>
    /// When true, body pages use the current chapter title as the header text
    /// (falls back to <see cref="Template"/> when no chapter is active).
    /// </summary>
    public bool UseChapterTitle { get; init; }
}
