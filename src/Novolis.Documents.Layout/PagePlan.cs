using Novolis.Documents;

namespace Novolis.Documents.Layout;

/// <summary>Measures text height for a given width and style (Skia or test fake).</summary>
public interface ITextMeasurer
{
    /// <summary>Returns the height in points required to draw <paramref name="text"/>.</summary>
    float MeasureHeight(string text, float widthPt, TextStyle style);
}

/// <summary>Text style passed to <see cref="ITextMeasurer"/>.</summary>
/// <param name="FontFamily">Font family name.</param>
/// <param name="FontSizePt">Size in points.</param>
/// <param name="LineHeight">Line height multiplier.</param>
/// <param name="Bold">When true, prefer a bold face.</param>
public readonly record struct TextStyle(
    string FontFamily,
    float FontSizePt,
    float LineHeight,
    bool Bold = false);

/// <summary>Kind of finished page.</summary>
public enum PageKind
{
    Cover,
    Toc,
    Body,
    Last,
}

/// <summary>A block placed on a page at a vertical offset within the content box.</summary>
/// <param name="Block">Source block.</param>
/// <param name="YInContentPt">Y offset from the top of the content rect.</param>
/// <param name="HeightPt">Occupied height.</param>
public sealed record PlacedBlock(IBlock Block, float YInContentPt, float HeightPt);

/// <summary>One finished page.</summary>
public sealed class PageSlice
{
    /// <summary>Page role.</summary>
    public required PageKind Kind { get; init; }

    /// <summary>1-based page number in the finished document.</summary>
    public required int Number { get; init; }

    /// <summary>Blocks drawn in the content area.</summary>
    public required IReadOnlyList<PlacedBlock> Blocks { get; init; }

    /// <summary>Whether to draw the running header.</summary>
    public bool ShowHeader { get; init; }

    /// <summary>Whether to draw the running footer.</summary>
    public bool ShowFooter { get; init; }
}

/// <summary>Complete pagination result.</summary>
public sealed class PagePlan
{
    /// <summary>Ordered pages.</summary>
    public required IReadOnlyList<PageSlice> Pages { get; init; }

    /// <summary>TOC entries with resolved page numbers (may be empty).</summary>
    public IReadOnlyList<TocEntry> TocEntries { get; init; } = [];
}
