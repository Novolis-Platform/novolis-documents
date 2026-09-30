namespace Novolis.Documents;

/// <summary>Centered scene-break ornament.</summary>
public sealed class SceneBreakBlock : IBlock
{
    /// <summary>Glyph or short ornament string.</summary>
    public string Ornament { get; init; } = "***";
}
