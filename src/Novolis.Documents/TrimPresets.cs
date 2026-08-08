using Novolis.Math.Measure;

namespace Novolis.Documents;

/// <summary>Common page trim sizes (inches / ISO).</summary>
public static class TrimPresets
{
    /// <summary>6×9 inches.</summary>
    public static Size Inch6x9 { get; } =
        new(LengthUnits.FromInches(6f), LengthUnits.FromInches(9f));

    /// <summary>5.5×8.5 inches.</summary>
    public static Size Inch5_5x8_5 { get; } =
        new(LengthUnits.FromInches(5.5f), LengthUnits.FromInches(8.5f));

    /// <summary>ISO A5.</summary>
    public static Size A5 { get; } =
        new(LengthUnits.FromMillimeters(148f), LengthUnits.FromMillimeters(210f));

    /// <summary>US Letter.</summary>
    public static Size USLetter { get; } =
        new(LengthUnits.FromInches(8.5f), LengthUnits.FromInches(11f));

    /// <summary>
    /// Print-oriented interior margins for a single-sided PDF preview of a bound page:
    /// larger binding edge (left), tighter outer (right), modest head/foot.
    /// Not Word’s 1″ uniform box.
    /// </summary>
    public static Thickness DefaultMargin { get; } =
        new(
            LengthUnits.FromInches(0.75f),
            LengthUnits.FromInches(0.5f),
            LengthUnits.FromInches(0.5f),
            LengthUnits.FromInches(0.65f));

    /// <summary>Uniform 1″ margins (report / Word-like).</summary>
    public static Thickness ReportMargin { get; } =
        Thickness.Uniform(LengthUnits.FromInches(1f));
}
