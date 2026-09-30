namespace Novolis.Documents;

/// <summary>How table rules are stroked.</summary>
public enum TableRuleStyle
{
    /// <summary>No strokes.</summary>
    None = 0,

    /// <summary>Full cell grid.</summary>
    Grid = 1,

    /// <summary>Header underline + row separators (invoice-style).</summary>
    Horizontal = 2,
}
