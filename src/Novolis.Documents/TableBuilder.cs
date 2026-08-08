namespace Novolis.Documents;

/// <summary>Fluent builder for <see cref="TableBlock"/>.</summary>
public sealed class TableBuilder
{
    readonly List<string> _headers = [];
    readonly List<IReadOnlyList<string>> _rows = [];
    float[]? _widths;
    CellAlign[]? _alignments;
    bool _showHeader = true;
    TableRuleStyle _ruleStyle = TableRuleStyle.Horizontal;
    bool _headerBackground = true;
    bool _repeatHeader = true;

    /// <summary>Sets header cells (enables the header row).</summary>
    public TableBuilder Headers(params string[] headers)
    {
        ArgumentNullException.ThrowIfNull(headers);
        _headers.Clear();
        _headers.AddRange(headers);
        _showHeader = headers.Length > 0;
        return this;
    }

    /// <summary>Appends one body row.</summary>
    public TableBuilder Row(params string[] cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        _rows.Add(cells);
        return this;
    }

    /// <summary>Appends many body rows.</summary>
    public TableBuilder Rows(IEnumerable<IReadOnlyList<string>> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        foreach (var row in rows)
            _rows.Add(row);
        return this;
    }

    /// <summary>Relative column widths (same count as columns).</summary>
    public TableBuilder ColumnWidths(params float[] widths)
    {
        ArgumentNullException.ThrowIfNull(widths);
        _widths = widths;
        return this;
    }

    /// <summary>Per-column alignment (same count as columns).</summary>
    public TableBuilder Align(params CellAlign[] alignments)
    {
        ArgumentNullException.ThrowIfNull(alignments);
        _alignments = alignments;
        return this;
    }

    /// <summary>Rule style (default <see cref="TableRuleStyle.Horizontal"/>).</summary>
    public TableBuilder Rules(TableRuleStyle style)
    {
        _ruleStyle = style;
        return this;
    }

    /// <summary>Whether to draw a header row.</summary>
    public TableBuilder ShowHeader(bool show = true)
    {
        _showHeader = show;
        return this;
    }

    /// <summary>Light header band fill.</summary>
    public TableBuilder HeaderBackground(bool enabled = true)
    {
        _headerBackground = enabled;
        return this;
    }

    /// <summary>Repeat header when the table breaks across pages.</summary>
    public TableBuilder RepeatHeaderOnPageBreak(bool repeat = true)
    {
        _repeatHeader = repeat;
        return this;
    }

    /// <summary>Builds an immutable <see cref="TableBlock"/>.</summary>
    public TableBlock Build() => new()
    {
        Headers = _headers.ToArray(),
        Rows = _rows.ToArray(),
        ColumnWidths = _widths,
        ColumnAlignments = _alignments,
        ShowHeader = _showHeader,
        RuleStyle = _ruleStyle,
        HeaderBackground = _headerBackground,
        RepeatHeaderOnPageBreak = _repeatHeader,
    };
}
