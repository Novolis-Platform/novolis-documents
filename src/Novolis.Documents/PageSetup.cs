using Novolis.Math.Measure;

namespace Novolis.Documents;

/// <summary>Physical page trim and margins.</summary>
public sealed class PageSetup
{
    /// <summary>Finished page size (trim).</summary>
    public required Size Trim { get; init; }

    /// <summary>Content margins inside the trim.</summary>
    public required Thickness Margin { get; init; }

    /// <summary>Height reserved for the running header band (points).</summary>
    public Length HeaderBand { get; init; } = LengthUnits.FromPoints(18f);

    /// <summary>Height reserved for the running footer band (points).</summary>
    public Length FooterBand { get; init; } = LengthUnits.FromPoints(18f);
}
