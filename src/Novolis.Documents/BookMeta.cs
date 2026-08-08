using Novolis.Math.Measure;

namespace Novolis.Documents;

/// <summary>Book metadata shown on the cover and optional chrome.</summary>
public sealed class BookMeta
{
    /// <summary>Primary title.</summary>
    public required string Title { get; init; }

    /// <summary>Optional subtitle.</summary>
    public string? Subtitle { get; init; }

    /// <summary>Optional series name.</summary>
    public string? Series { get; init; }

    /// <summary>Optional author line.</summary>
    public string? Author { get; init; }

    /// <summary>Optional rights / copyright line.</summary>
    public string? Rights { get; init; }
}
