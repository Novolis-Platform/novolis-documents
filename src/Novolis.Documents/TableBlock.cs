namespace Novolis.Documents;

/// <summary>Simple grid table (string cells).</summary>
public sealed class TableBlock : IBlock
{
    /// <summary>Header cell texts (optional).</summary>
    public IReadOnlyList<string> Headers { get; init; } = [];

    /// <summary>Body rows; each row is a sequence of cell texts.</summary>
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = [];

    /// <summary>
    /// Optional relative column widths (same count as columns). When null/empty, columns share width equally.
    /// </summary>
    public IReadOnlyList<float>? ColumnWidths { get; init; }

    /// <summary>Optional per-column alignment (same count as columns). Defaults to Left.</summary>
    public IReadOnlyList<CellAlign>? ColumnAlignments { get; init; }

    /// <summary>When true and <see cref="Headers"/> is non-empty, draw a header row.</summary>
    public bool ShowHeader { get; init; } = true;

    /// <summary>When true, stroke cell rules (legacy full grid). Prefer <see cref="RuleStyle"/>.</summary>
    public bool DrawRules
    {
        get => RuleStyle != TableRuleStyle.None;
        init => RuleStyle = value ? TableRuleStyle.Grid : TableRuleStyle.None;
    }

    /// <summary>Rule drawing style. Defaults to grid when <see cref="DrawRules"/> is set true via init.</summary>
    public TableRuleStyle RuleStyle { get; init; } = TableRuleStyle.None;

    /// <summary>When true, fill the header row with a light band.</summary>
    public bool HeaderBackground { get; init; }

    /// <summary>Repeat header row when the table breaks across pages.</summary>
    public bool RepeatHeaderOnPageBreak { get; init; } = true;
}
