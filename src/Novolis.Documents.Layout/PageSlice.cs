using Novolis.Documents;

namespace Novolis.Documents.Layout;

/// <summary>One finished page.</summary>
public sealed class PageSlice
{
    /// <summary>Page role.</summary>
    public required PageKind Kind { get; init; }

    /// <summary>1-based page number in the finished document.</summary>
    public required int Number { get; init; }

    /// <summary>Blocks drawn in the content area.</summary>
    public required IReadOnlyList<PlacedBlock> Blocks { get; init; }

    /// <summary>Whether to draw the page header.</summary>
    public bool ShowHeader { get; init; }

    /// <summary>Whether to draw the page footer.</summary>
    public bool ShowFooter { get; init; }

    /// <summary>Current chapter title for chapter-style headers (null when none).</summary>
    public string? ChapterTitle { get; init; }
}
