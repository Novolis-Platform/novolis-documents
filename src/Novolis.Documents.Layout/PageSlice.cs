using Novolis.Documents;

namespace Novolis.Documents.Layout;

/// <summary>One finished page.</summary>
public sealed class PageSlice
{
    /// <summary>Page role.</summary>
    public required PageKind Kind { get; init; }

    /// <summary>1-based page number in the finished document.</summary>
    public required int Number { get; init; }

    /// <summary>Semantic fragments drawn in the content area.</summary>
    public IReadOnlyList<PlacedFragment> Fragments { get; set; } = [];

    /// <summary>Legacy blocks drawn in the content area.</summary>
    public IReadOnlyList<PlacedBlock> Blocks { get; set; } = [];

    /// <summary>Whether to draw the page header.</summary>
    public bool ShowHeader { get; set; }

    /// <summary>Whether to draw the page footer.</summary>
    public bool ShowFooter { get; set; }

    /// <summary>Current chapter title for chapter-style headers (null when none).</summary>
    public string? ChapterTitle { get; set; }
}
