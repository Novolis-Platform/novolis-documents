namespace Novolis.Documents;

/// <summary>Document metadata for the first page, chrome placeholders, and exporters.</summary>
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

    /// <summary>Optional contributors (editors, illustrators, …).</summary>
    public string? Contributors { get; init; }

    /// <summary>Optional publisher / imprint.</summary>
    public string? Publisher { get; init; }

    /// <summary>Optional subject / topic.</summary>
    public string? Subject { get; init; }

    /// <summary>Optional longer description / abstract.</summary>
    public string? Description { get; init; }

    /// <summary>Optional keywords.</summary>
    public IReadOnlyList<string> Keywords { get; init; } = [];

    /// <summary>Optional document / catalog identifier (ISBN, DOI, internal id, …).</summary>
    public string? Identifier { get; init; }

    /// <summary>Optional language tag (e.g. <c>en</c>, <c>nb-NO</c>).</summary>
    public string? Language { get; init; }

    /// <summary>Optional edition or version label.</summary>
    public string? Version { get; init; }

    /// <summary>Optional publication or issue date.</summary>
    public DateOnly? Date { get; init; }

    /// <summary>Optional rights / copyright line.</summary>
    public string? Rights { get; init; }
}
