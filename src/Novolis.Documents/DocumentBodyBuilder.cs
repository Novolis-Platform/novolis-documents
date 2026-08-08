namespace Novolis.Documents;

/// <summary>
/// Body spine: <see cref="First"/> → <see cref="Content"/> → <see cref="Last"/>.
/// </summary>
public sealed class DocumentBodyBuilder
{
    readonly DocumentContentBuilder _content = new();

    internal FirstPage? FirstPage { get; private set; }
    internal LastPage? LastPage { get; private set; }
    internal bool IncludeCover { get; private set; }
    internal bool IncludeToc => _content.IncludeToc;
    internal IReadOnlyList<IBlock> ContentBlocks => _content.ToBlocks();

    /// <summary>Opening (title) page.</summary>
    public DocumentBodyBuilder First(Action<FirstPageBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new FirstPageBuilder();
        configure(builder);
        FirstPage = builder.Build();
        IncludeCover = true;
        return this;
    }

    /// <summary>Main flowing content (chapters, paragraphs, tables, …).</summary>
    public DocumentBodyBuilder Content(Action<DocumentContentBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_content);
        return this;
    }

    /// <summary>Closing page after the main content.</summary>
    public DocumentBodyBuilder Last(Action<LastPageBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new LastPageBuilder();
        configure(builder);
        LastPage = builder.Build();
        return this;
    }
}
