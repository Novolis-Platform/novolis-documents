namespace Novolis.Documents;

/// <summary>
/// Immutable paged document: data model only (no fluent layout DSL).
/// Consumers map Markdown or other sources into this shape.
/// </summary>
public sealed class PagedDocument
{
    /// <summary>Cover / chrome metadata.</summary>
    public required DocumentMeta Meta { get; init; }

    /// <summary>Trim and margins.</summary>
    public required PageSetup Setup { get; init; }

    /// <summary>Type sizes and fonts.</summary>
    public required Typography Typography { get; init; }

    /// <summary>Body blocks in reading order (do not include cover; use <see cref="IncludeCover"/>).</summary>
    public required IReadOnlyList<IBlock> Body { get; init; }

    /// <summary>Running header template (body pages).</summary>
    public RunningChrome? Header { get; init; }

    /// <summary>Running footer template (body and TOC pages).</summary>
    public RunningChrome? Footer { get; init; }

    /// <summary>When true, emit a cover as page 1.</summary>
    public bool IncludeCover { get; init; } = true;

    /// <summary>When true, insert a TOC after the cover from H1 titles.</summary>
    public bool IncludeToc { get; init; }

    /// <summary>Optional last page after body.</summary>
    public LastPage? Last { get; init; }

    /// <summary>When true, suppress header on pages that open with an H1.</summary>
    public bool SuppressHeaderOnH1Open { get; init; } = true;
}
