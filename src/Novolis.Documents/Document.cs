namespace Novolis.Documents;

/// <summary>
/// Immutable semantic document root. The document flow is a vertical sequence of sections.
/// </summary>
public sealed class Document
{
    /// <summary>Title and bibliographic metadata.</summary>
    public required DocumentMeta Meta { get; init; }

    /// <summary>Trim size, margins, and reserved page chrome bands.</summary>
    public required PageSetup Setup { get; init; }

    /// <summary>Typography defaults used by layout and rendering.</summary>
    public required Typography Typography { get; init; }

    /// <summary>Main document flow in reading order.</summary>
    public IReadOnlyList<ISection> Sections { get; init; } = [];

    /// <summary>Optional dedicated first-page definition.</summary>
    public PageDefinition? FirstPage { get; init; }

    /// <summary>Optional dedicated last-page definition.</summary>
    public PageDefinition? LastPage { get; init; }

    /// <summary>Compatibility flag for builders that still use the earlier body spine.</summary>
    public bool IncludeCover { get; init; }

    /// <summary>Compatibility view of blocks supplied through the earlier body spine.</summary>
    public IReadOnlyList<IBlock> Body { get; init; } = [];

    /// <summary>Optional page header.</summary>
    public Header? Header { get; init; }

    /// <summary>Optional page footer.</summary>
    public Footer? Footer { get; init; }

    /// <summary>Optional watermark configuration.</summary>
    public Watermark? Watermark { get; init; }

    /// <summary>Document-wide semantic style defaults.</summary>
    public DocumentTheme Theme { get; init; } = DocumentTheme.Default;

    /// <summary>Collect level-one headings into the PDF outline.</summary>
    public bool IncludeToc { get; init; }

    /// <summary>Whether a first page has been configured.</summary>
    public bool HasFirstPage => FirstPage is not null || IncludeCover;

    /// <summary>Starts an empty document builder.</summary>
    public static DocumentBuilder Create() => new();

    /// <summary>Starts a document builder with a title.</summary>
    public static DocumentBuilder Create(string title) => new DocumentBuilder().Title(title);
}
