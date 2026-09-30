namespace Novolis.Documents;

/// <summary>Contents-page placeholder; entries are filled by layout from level-1 headings.</summary>
public sealed class TocBlock : IBlock
{
    /// <summary>Optional pre-supplied entries; when empty, layout collects level-1 headings.</summary>
    public IReadOnlyList<TocEntry> Entries { get; init; } = [];
}
