using Novolis.Documents;
using Novolis.Documents.Layout;
using Novolis.Math.Measure;
using SkiaSharp;

namespace Novolis.Documents.Skia;

/// <summary>Options for PDF generation.</summary>
public sealed class DocumentPdfOptions
{
    /// <summary>Optional path to a body TrueType/OpenType font file. Overrides the embedded Liberation Serif subset.</summary>
    public string? BodyFontPath { get; init; }

    /// <summary>Optional path to a bold font file. Overrides the embedded Liberation Serif Bold subset.</summary>
    public string? BoldFontPath { get; init; }
}
