using Novolis.Documents;

namespace Novolis.Documents.Layout;

/// <summary>Text style passed to <see cref="ITextMeasurer"/>.</summary>
/// <param name="FontFamily">Font family name.</param>
/// <param name="FontSizePt">Size in points.</param>
/// <param name="LineHeight">Line height multiplier.</param>
/// <param name="Bold">When true, prefer a bold face.</param>
public readonly record struct TextStyle(
    string FontFamily,
    float FontSizePt,
    float LineHeight,
    bool Bold = false);
