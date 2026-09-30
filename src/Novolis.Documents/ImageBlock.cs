namespace Novolis.Documents;

/// <summary>Raster or SVG image drawn at a fixed point size.</summary>
public sealed class ImageBlock : IBlock
{
    /// <summary>Absolute or relative path to PNG/JPEG/SVG. Ignored when <see cref="Data"/> is set.</summary>
    public string? Path { get; init; }

    /// <summary>Raw image bytes (PNG/JPEG/SVG). Takes precedence over <see cref="Path"/>.</summary>
    public byte[]? Data { get; init; }

    /// <summary>Draw width in points.</summary>
    public required float WidthPt { get; init; }

    /// <summary>Draw height in points.</summary>
    public required float HeightPt { get; init; }
}
