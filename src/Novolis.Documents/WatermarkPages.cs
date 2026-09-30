namespace Novolis.Documents;

/// <summary>Which page regions receive a watermark.</summary>
[Flags]
public enum WatermarkPages
{
    /// <summary>No pages.</summary>
    None = 0,

    /// <summary>Opening / title page(s).</summary>
    First = 1,

    /// <summary>Table-of-contents pages.</summary>
    Toc = 2,

    /// <summary>Main content pages.</summary>
    Body = 4,

    /// <summary>Closing page(s).</summary>
    Last = 8,

    /// <summary>Every page.</summary>
    All = First | Toc | Body | Last,
}
