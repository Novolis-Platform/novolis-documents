namespace Novolis.Documents;

/// <summary>Fluent builder for <see cref="DocumentMeta"/>.</summary>
public sealed class DocumentMetaBuilder
{
    string _title = "Untitled";
    string? _subtitle;
    string? _series;
    string? _author;
    string? _contributors;
    string? _publisher;
    string? _subject;
    string? _description;
    readonly List<string> _keywords = [];
    string? _identifier;
    string? _language;
    string? _version;
    DateOnly? _date;
    string? _rights;

    /// <summary>Seeds from an existing meta snapshot.</summary>
    public DocumentMetaBuilder From(DocumentMeta meta)
    {
        ArgumentNullException.ThrowIfNull(meta);
        _title = meta.Title;
        _subtitle = meta.Subtitle;
        _series = meta.Series;
        _author = meta.Author;
        _contributors = meta.Contributors;
        _publisher = meta.Publisher;
        _subject = meta.Subject;
        _description = meta.Description;
        _keywords.Clear();
        _keywords.AddRange(meta.Keywords);
        _identifier = meta.Identifier;
        _language = meta.Language;
        _version = meta.Version;
        _date = meta.Date;
        _rights = meta.Rights;
        return this;
    }

    /// <summary>Primary title.</summary>
    public DocumentMetaBuilder Title(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        _title = title;
        return this;
    }

    /// <summary>Subtitle.</summary>
    public DocumentMetaBuilder Subtitle(string? subtitle)
    {
        _subtitle = subtitle;
        return this;
    }

    /// <summary>Series.</summary>
    public DocumentMetaBuilder Series(string? series)
    {
        _series = series;
        return this;
    }

    /// <summary>Author.</summary>
    public DocumentMetaBuilder Author(string? author)
    {
        _author = author;
        return this;
    }

    /// <summary>Contributors.</summary>
    public DocumentMetaBuilder Contributors(string? contributors)
    {
        _contributors = contributors;
        return this;
    }

    /// <summary>Publisher / imprint.</summary>
    public DocumentMetaBuilder Publisher(string? publisher)
    {
        _publisher = publisher;
        return this;
    }

    /// <summary>Subject / topic.</summary>
    public DocumentMetaBuilder Subject(string? subject)
    {
        _subject = subject;
        return this;
    }

    /// <summary>Description / abstract.</summary>
    public DocumentMetaBuilder Description(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>Keywords (replaces prior list).</summary>
    public DocumentMetaBuilder Keywords(params string[] keywords)
    {
        ArgumentNullException.ThrowIfNull(keywords);
        _keywords.Clear();
        _keywords.AddRange(keywords.Where(static k => !string.IsNullOrWhiteSpace(k)));
        return this;
    }

    /// <summary>Document identifier (ISBN, DOI, …).</summary>
    public DocumentMetaBuilder Identifier(string? identifier)
    {
        _identifier = identifier;
        return this;
    }

    /// <summary>Language tag.</summary>
    public DocumentMetaBuilder Language(string? language)
    {
        _language = language;
        return this;
    }

    /// <summary>Edition / version label.</summary>
    public DocumentMetaBuilder Version(string? version)
    {
        _version = version;
        return this;
    }

    /// <summary>Publication or issue date.</summary>
    public DocumentMetaBuilder Date(DateOnly? date)
    {
        _date = date;
        return this;
    }

    /// <summary>Rights / copyright.</summary>
    public DocumentMetaBuilder Rights(string? rights)
    {
        _rights = rights;
        return this;
    }

    /// <summary>Builds metadata.</summary>
    public DocumentMeta Build() => new()
    {
        Title = _title,
        Subtitle = _subtitle,
        Series = _series,
        Author = _author,
        Contributors = _contributors,
        Publisher = _publisher,
        Subject = _subject,
        Description = _description,
        Keywords = _keywords.ToArray(),
        Identifier = _identifier,
        Language = _language,
        Version = _version,
        Date = _date,
        Rights = _rights,
    };
}
