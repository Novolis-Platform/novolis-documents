namespace Novolis.Documents;

/// <summary>Running header or footer template.</summary>
public sealed class RunningChrome
{
    /// <summary>
    /// Template text. Supports <c>{page}</c> and <c>{title}</c> placeholders.
    /// Empty string means no text in that chrome band.
    /// </summary>
    public string Template { get; init; } = string.Empty;

    /// <summary>Font size in points for chrome text.</summary>
    public float FontSizePt { get; init; } = 9f;
}
