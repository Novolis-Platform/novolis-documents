namespace Novolis.Documents;

/// <summary>Optional final page after body flow.</summary>
public sealed class LastPage
{
    /// <summary>Optional title drawn at the top of the last page.</summary>
    public string? Title { get; init; }

    /// <summary>Colophon / closing lines (plain paragraphs).</summary>
    public IReadOnlyList<string> Lines { get; init; } = [];

    /// <summary>Optional richer blocks after <see cref="Lines"/> (paragraphs, tables, etc.).</summary>
    public IReadOnlyList<IBlock> Blocks { get; init; } = [];
}
