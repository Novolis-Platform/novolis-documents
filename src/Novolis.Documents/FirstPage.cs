namespace Novolis.Documents;

/// <summary>Optional opening (title) page before body flow.</summary>
public sealed class FirstPage
{
    /// <summary>Title line; falls back to <see cref="DocumentMeta.Title"/>.</summary>
    public string? Title { get; init; }

    /// <summary>Subtitle line; falls back to <see cref="DocumentMeta.Subtitle"/>.</summary>
    public string? Subtitle { get; init; }

    /// <summary>Series line; falls back to <see cref="DocumentMeta.Series"/>.</summary>
    public string? Series { get; init; }

    /// <summary>Author line; falls back to <see cref="DocumentMeta.Author"/>.</summary>
    public string? Author { get; init; }

    /// <summary>Rights / imprint line; falls back to <see cref="DocumentMeta.Rights"/>.</summary>
    public string? Rights { get; init; }

    /// <summary>Additional centered lines below the author block.</summary>
    public IReadOnlyList<string> Lines { get; init; } = [];
}
