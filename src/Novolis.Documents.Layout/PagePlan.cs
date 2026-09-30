using Novolis.Documents;

namespace Novolis.Documents.Layout;

/// <summary>Complete pagination result.</summary>
public sealed class PagePlan
{
    /// <summary>Ordered pages.</summary>
    public required IReadOnlyList<PageSlice> Pages { get; init; }

    /// <summary>TOC entries with resolved page numbers (may be empty).</summary>
    public IReadOnlyList<TocEntry> TocEntries { get; init; } = [];
}
