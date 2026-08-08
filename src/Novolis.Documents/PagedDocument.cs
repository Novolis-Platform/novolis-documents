namespace Novolis.Documents;

/// <summary>
/// Immutable paged document model.
/// Prefer <see cref="Document.Create"/> / <see cref="DocumentBuilder"/> for fluent construction; mappers may also build this shape directly.
/// </summary>
public sealed class PagedDocument
{
    /// <summary>Cover / chrome metadata.</summary>
    public required DocumentMeta Meta { get; init; }

    /// <summary>Trim and margins.</summary>
    public required PageSetup Setup { get; init; }

    /// <summary>Type sizes and fonts.</summary>
    public required Typography Typography { get; init; }

    /// <summary>Body blocks in reading order (do not put cover content here; use <see cref="First"/> / <see cref="IncludeCover"/>).</summary>
    public required IReadOnlyList<IBlock> Body { get; init; }

    /// <summary>Running header template (body pages).</summary>
    public RunningChrome? Header { get; init; }

    /// <summary>Running footer template (body and contents pages).</summary>
    public RunningChrome? Footer { get; init; }

    /// <summary>
    /// Optional opening (title) page. When set, a first page is emitted even if <see cref="IncludeCover"/> is false.
    /// Fields fall back to <see cref="Meta"/> when null.
    /// </summary>
    public FirstPage? First { get; init; }

    /// <summary>When true, emit a first/title page from <see cref="Meta"/> (and <see cref="First"/> when present).</summary>
    public bool IncludeCover { get; init; } = true;

    /// <summary>When true, insert a contents page after the first page from level-1 headings.</summary>
    public bool IncludeToc { get; init; }

    /// <summary>Optional closing page after body.</summary>
    public LastPage? Last { get; init; }

    /// <summary>When true, suppress header on pages that open with a level-1 heading.</summary>
    public bool SuppressHeaderOnLevel1Open { get; init; } = true;

    /// <summary>True when a first/title page should be emitted.</summary>
    public bool HasFirstPage => IncludeCover || First is not null;
}
