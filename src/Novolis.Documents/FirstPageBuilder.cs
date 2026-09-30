namespace Novolis.Documents;

/// <summary>Fluent builder for <see cref="FirstPage"/>.</summary>
public sealed class FirstPageBuilder
{
    string? _title;
    string? _subtitle;
    string? _series;
    string? _author;
    string? _rights;
    readonly List<string> _lines = [];
    readonly DocumentContentBuilder _blocks = new();

    /// <summary>Title override.</summary>
    public FirstPageBuilder Title(string? title)
    {
        _title = title;
        return this;
    }

    /// <summary>Subtitle override.</summary>
    public FirstPageBuilder Subtitle(string? subtitle)
    {
        _subtitle = subtitle;
        return this;
    }

    /// <summary>Series override.</summary>
    public FirstPageBuilder Series(string? series)
    {
        _series = series;
        return this;
    }

    /// <summary>Author override.</summary>
    public FirstPageBuilder Author(string? author)
    {
        _author = author;
        return this;
    }

    /// <summary>Rights override.</summary>
    public FirstPageBuilder Rights(string? rights)
    {
        _rights = rights;
        return this;
    }

    /// <summary>Additional lines below the title block.</summary>
    public FirstPageBuilder Lines(params string[] lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        _lines.AddRange(lines);
        return this;
    }

    /// <summary>Richer blocks after lines (may overflow onto further First pages).</summary>
    public FirstPageBuilder Blocks(Action<DocumentContentBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_blocks);
        return this;
    }

    /// <summary>Builds the first page.</summary>
    public FirstPage Build() => new()
    {
        Title = _title,
        Subtitle = _subtitle,
        Series = _series,
        Author = _author,
        Rights = _rights,
        Lines = _lines.ToArray(),
        Blocks = _blocks.ToBlocks(),
    };
}
