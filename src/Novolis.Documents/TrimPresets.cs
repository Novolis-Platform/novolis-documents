using Novolis.Math.Measure;

namespace Novolis.Documents;

/// <summary>Customary book trim sizes.</summary>
public static class TrimPresets
{
    /// <summary>Trade paperback 6×9 inches (ManuscriptPrintSettings default).</summary>
    public static Size TradePaperback6x9 { get; } =
        new(LengthUnits.FromInches(6f), LengthUnits.FromInches(9f));

    /// <summary>Digest 5.5×8.5 inches.</summary>
    public static Size Digest5_5x8_5 { get; } =
        new(LengthUnits.FromInches(5.5f), LengthUnits.FromInches(8.5f));

    /// <summary>ISO A5.</summary>
    public static Size A5 { get; } =
        new(LengthUnits.FromMillimeters(148f), LengthUnits.FromMillimeters(210f));

    /// <summary>US Letter (allowed, not book-primary).</summary>
    public static Size USLetter { get; } =
        new(LengthUnits.FromInches(8.5f), LengthUnits.FromInches(11f));

    /// <summary>Default manuscript-style margins (slightly tighter right).</summary>
    public static Thickness DefaultBookMargin { get; } =
        new(
            LengthUnits.FromInches(0.65f),
            LengthUnits.FromInches(0.75f),
            LengthUnits.FromInches(0.55f),
            LengthUnits.FromInches(0.75f));
}
