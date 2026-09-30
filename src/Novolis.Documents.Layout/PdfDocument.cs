using Novolis.Documents;

namespace Novolis.Documents.Layout;

/// <summary>
/// Finished paged PDF: metadata, laid-out pages, and the outline tree written with the file.
/// </summary>
public sealed class PdfDocument
{
    /// <summary>Bibliographic metadata copied from the paged source.</summary>
    public required DocumentMeta Meta { get; init; }

    /// <summary>Laid-out pages.</summary>
    public required PagePlan Plan { get; init; }

    /// <summary>Outline bookmarks (may be empty).</summary>
    public IReadOnlyList<PdfOutlineEntry> Outlines { get; init; } = [];

    /// <summary>
    /// Builds a PDF model from a finished page plan. Outline is each level-1 heading
    /// with its resolved page number (no painted contents page).
    /// </summary>
    public static PdfDocument From(DocumentMeta meta, PagePlan plan)
    {
        ArgumentNullException.ThrowIfNull(meta);
        ArgumentNullException.ThrowIfNull(plan);
        return new PdfDocument
        {
            Meta = meta,
            Plan = plan,
            Outlines = BuildFromPlan(plan),
        };
    }

    /// <summary>
    /// Builds a PDF model with an explicit outline tree. Empty titles and page numbers
    /// below 1 are dropped. Page numbers that fall outside the plan are kept only when
    /// they still resolve after layout (callers should pass numbers from the same plan).
    /// </summary>
    public static PdfDocument From(
        DocumentMeta meta,
        PagePlan plan,
        IReadOnlyList<PdfOutlineEntry> outlines)
    {
        ArgumentNullException.ThrowIfNull(meta);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(outlines);
        return new PdfDocument
        {
            Meta = meta,
            Plan = plan,
            Outlines = Sanitize(outlines),
        };
    }

    static IReadOnlyList<PdfOutlineEntry> BuildFromPlan(PagePlan plan)
    {
        var entries = new List<PdfOutlineEntry>();
        foreach (var entry in plan.TocEntries)
        {
            if (string.IsNullOrWhiteSpace(entry.Title) || entry.PageNumber < 1)
                continue;
            entries.Add(new PdfOutlineEntry(entry.Title.Trim(), entry.PageNumber));
        }

        return entries;
    }

    static IReadOnlyList<PdfOutlineEntry> Sanitize(IReadOnlyList<PdfOutlineEntry> outlines)
    {
        var result = new List<PdfOutlineEntry>(outlines.Count);
        foreach (var entry in outlines)
        {
            if (string.IsNullOrWhiteSpace(entry.Title) || entry.PageNumber < 1)
                continue;
            var children = entry.Children is { Count: > 0 } nested
                ? Sanitize(nested)
                : Array.Empty<PdfOutlineEntry>();
            result.Add(new PdfOutlineEntry(entry.Title.Trim(), entry.PageNumber, children));
        }

        return result;
    }
}
