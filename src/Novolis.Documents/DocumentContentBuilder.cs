namespace Novolis.Documents;

/// <summary>
/// Fluent accumulator for a vertical list of <see cref="IBlock"/> (content flow, column, or last-page blocks).
/// </summary>
public sealed class DocumentContentBuilder
{
    readonly List<IBlock> _blocks = [];

    /// <summary>Blocks accumulated so far (live view).</summary>
    public IReadOnlyList<IBlock> Blocks => _blocks;

    /// <summary>When true, emit a table-of-contents page from chapter / level-1 headings.</summary>
    public bool IncludeToc { get; private set; }

    /// <summary>Request a TOC page before the main content flow.</summary>
    public DocumentContentBuilder Toc(bool include = true)
    {
        IncludeToc = include;
        return this;
    }

    /// <summary>
    /// Chapter: level-1 heading (page break when prior content exists), then optional nested blocks.
    /// </summary>
    public DocumentContentBuilder Chapter(string title, Action<DocumentContentBuilder>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Heading(1, title);
        if (configure is null)
            return this;

        var nested = new DocumentContentBuilder();
        configure(nested);
        return AddRange(nested.ToBlocks());
    }

    /// <summary>Appends an arbitrary block.</summary>
    public DocumentContentBuilder Add(IBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);
        _blocks.Add(block);
        return this;
    }

    /// <summary>Appends several blocks.</summary>
    public DocumentContentBuilder AddRange(IEnumerable<IBlock> blocks)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        foreach (var block in blocks)
            Add(block);
        return this;
    }

    /// <summary>Heading levels 1–3. Level 1 forces a page break when prior content exists.</summary>
    public DocumentContentBuilder Heading(int level, string text)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(level, 3);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        return Add(new HeadingBlock { Level = level, Text = text });
    }

    /// <summary>Level-1 heading (prefer <see cref="Chapter"/> for named chapters).</summary>
    public DocumentContentBuilder H1(string text) => Heading(1, text);

    /// <summary>Level-2 heading.</summary>
    public DocumentContentBuilder H2(string text) => Heading(2, text);

    /// <summary>Level-3 heading.</summary>
    public DocumentContentBuilder H3(string text) => Heading(3, text);

    /// <summary>Plain paragraph.</summary>
    public DocumentContentBuilder Paragraph(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Add(new ParagraphBlock { Text = text });
    }

    /// <summary>Builds and appends a table.</summary>
    public DocumentContentBuilder Table(Action<TableBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var table = new TableBuilder();
        configure(table);
        return Add(table.Build());
    }

    /// <summary>Image from a file path.</summary>
    public DocumentContentBuilder Image(string path, float widthPt, float heightPt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Add(new ImageBlock { Path = path, WidthPt = widthPt, HeightPt = heightPt });
    }

    /// <summary>Image from raw bytes (PNG/JPEG/SVG).</summary>
    public DocumentContentBuilder Image(byte[] data, float widthPt, float heightPt)
    {
        ArgumentNullException.ThrowIfNull(data);
        return Add(new ImageBlock { Data = data, WidthPt = widthPt, HeightPt = heightPt });
    }

    /// <summary>Side-by-side columns.</summary>
    public DocumentContentBuilder Columns(Action<ColumnsBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var columns = new ColumnsBuilder();
        configure(columns);
        return Add(columns.Build());
    }

    /// <summary>Centered scene-break ornament.</summary>
    public DocumentContentBuilder SceneBreak(string ornament = "***")
    {
        ArgumentNullException.ThrowIfNull(ornament);
        return Add(new SceneBreakBlock { Ornament = ornament });
    }

    /// <summary>Forced line break (one blank body line). Prefer <c>\n</c> inside <see cref="Paragraph"/> for soft wraps.</summary>
    public DocumentContentBuilder LineBreak() => Add(new LineBreakBlock());

    /// <summary>Explicit page break (continues the body on a new page).</summary>
    public DocumentContentBuilder PageBreak() => Add(new PageBreakBlock());

    /// <summary>Blank page.</summary>
    public DocumentContentBuilder BlankPage() => Add(new BlankPageBlock());

    /// <summary>Materializes a snapshot of accumulated blocks.</summary>
    public IReadOnlyList<IBlock> ToBlocks() => _blocks.ToArray();
}
