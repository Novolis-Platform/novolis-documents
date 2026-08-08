namespace Novolis.Documents;

/// <summary>Opaque RGB color for document paint (opacity is applied separately where needed).</summary>
public readonly record struct DocumentColor(byte R, byte G, byte B)
{
    /// <summary>Neutral gray.</summary>
    public static DocumentColor Gray { get; } = new(0x40, 0x40, 0x40);

    /// <summary>Black.</summary>
    public static DocumentColor Black { get; } = new(0, 0, 0);

    /// <summary>Red.</summary>
    public static DocumentColor Red { get; } = new(0xC0, 0x20, 0x20);

    /// <summary>Creates a color from 0–255 channels.</summary>
    public static DocumentColor FromRgb(byte r, byte g, byte b) => new(r, g, b);

    /// <summary>
    /// Parses <c>#RGB</c>, <c>#RRGGBB</c>, or <c>#AARRGGBB</c> (alpha ignored — use watermark opacity).
    /// </summary>
    public static DocumentColor Parse(string hex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hex);
        var s = hex.Trim();
        if (s.StartsWith('#'))
            s = s[1..];

        if (s.Length == 3)
        {
            var r = Convert.ToByte(new string(s[0], 2), 16);
            var g = Convert.ToByte(new string(s[1], 2), 16);
            var b = Convert.ToByte(new string(s[2], 2), 16);
            return new DocumentColor(r, g, b);
        }

        if (s.Length == 6)
        {
            return new DocumentColor(
                Convert.ToByte(s[..2], 16),
                Convert.ToByte(s[2..4], 16),
                Convert.ToByte(s[4..6], 16));
        }

        if (s.Length == 8)
        {
            return new DocumentColor(
                Convert.ToByte(s[2..4], 16),
                Convert.ToByte(s[4..6], 16),
                Convert.ToByte(s[6..8], 16));
        }

        throw new FormatException($"Expected #RGB, #RRGGBB, or #AARRGGBB, got '{hex}'.");
    }
}
