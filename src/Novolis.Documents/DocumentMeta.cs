namespace Novolis.Documents;

/// <summary>Document metadata shown on the cover and optional chrome.</summary>
public sealed class DocumentMeta
{
    /// <summary>Primary title.</summary>
    public required string Title { get; init; }

    /// <summary>Optional subtitle.</summary>
    public string? Subtitle { get; init; }

    /// <summary>Optional series or collection name.</summary>
    public string? Series { get; init; }

    /// <summary>Optional author line.</summary>
    public string? Author { get; init; }

    /// <summary>Optional rights / copyright line.</summary>
    public string? Rights { get; init; }
}
