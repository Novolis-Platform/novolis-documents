namespace Novolis.Documents;

/// <summary>Fluent builder for <see cref="ColumnsBlock"/>.</summary>
public sealed class ColumnsBuilder
{
    readonly List<IReadOnlyList<IBlock>> _columns = [];
    float _gapPt = 16f;
    float[]? _fractions;

    /// <summary>Gap between columns in points.</summary>
    public ColumnsBuilder Gap(float gapPt)
    {
        _gapPt = gapPt;
        return this;
    }

    /// <summary>Relative column widths (same count as columns).</summary>
    public ColumnsBuilder Fractions(params float[] fractions)
    {
        ArgumentNullException.ThrowIfNull(fractions);
        _fractions = fractions;
        return this;
    }

    /// <summary>Adds one column of stacked blocks.</summary>
    public ColumnsBuilder Column(Action<DocumentContentBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var content = new DocumentContentBuilder();
        configure(content);
        _columns.Add(content.ToBlocks());
        return this;
    }

    /// <summary>Builds an immutable <see cref="ColumnsBlock"/>.</summary>
    public ColumnsBlock Build()
    {
        if (_columns.Count == 0)
            throw new InvalidOperationException("Columns require at least one Column(…).");

        return new ColumnsBlock
        {
            Columns = _columns.ToArray(),
            GapPt = _gapPt,
            Fractions = _fractions,
        };
    }
}
