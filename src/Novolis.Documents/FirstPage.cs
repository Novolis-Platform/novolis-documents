namespace Novolis.Documents;

/// <summary>
/// Optional opening (title) section before body flow.
/// Usually one page; layout continues onto further First pages when content overflows.
/// </summary>
public sealed class FirstPage
{
    /// <summary>Title line; falls back to <see cref="DocumentMeta.Title"/>.</summary>
    public string? Title { get; init; }

    /// <summary>Subtitle line; falls back to <see cref="DocumentMeta.Subtitle"/>.</summary>
    public string? Subtitle { get; init; }

    /// <summary>Series line; falls back to <see cref="DocumentMeta.Series"/>.</summary>
    public string? Series { get; init; }

    /// <summary>Author line; falls back to <see cref="DocumentMeta.Author"/>.</summary>
    public string? Author { get; init; }

    /// <summary>Rights / imprint line; falls back to <see cref="DocumentMeta.Rights"/>.</summary>
    public string? Rights { get; init; }

    /// <summary>Additional lines below the title block.</summary>
    public IReadOnlyList<string> Lines { get; init; } = [];

    /// <summary>Optional richer blocks after <see cref="Lines"/> (paragraphs, tables, breaks, etc.).</summary>
    public IReadOnlyList<IBlock> Blocks { get; init; } = [];
}
