namespace Novolis.Documents;

/// <summary>Side-by-side columns of blocks (e.g. seller | buyer).</summary>
public sealed class ColumnsBlock : IBlock
{
    /// <summary>Columns; each is a vertical stack of blocks.</summary>
    public required IReadOnlyList<IReadOnlyList<IBlock>> Columns { get; init; }

    /// <summary>Gap between columns in points.</summary>
    public float GapPt { get; init; } = 16f;

    /// <summary>
    /// Optional relative column widths (same count as <see cref="Columns"/>). When null, equal widths.
    /// </summary>
    public IReadOnlyList<float>? Fractions { get; init; }
}
