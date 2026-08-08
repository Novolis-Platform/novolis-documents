namespace Novolis.Documents;

/// <summary>Running header or footer template.</summary>
public sealed class RunningChrome
{
    /// <summary>
    /// Template text. Placeholders: <c>{page}</c>, <c>{pages}</c>, <c>{title}</c>, <c>{subtitle}</c>,
    /// <c>{author}</c>, <c>{series}</c>, <c>{publisher}</c>, <c>{subject}</c>, <c>{identifier}</c>,
    /// <c>{version}</c>, <c>{language}</c>, <c>{date}</c>, <c>{rights}</c>.
    /// Empty string means no text in that chrome band.
    /// </summary>
    public string Template { get; init; } = string.Empty;

    /// <summary>Font size in points for chrome text.</summary>
    public float FontSizePt { get; init; } = 9f;
}
