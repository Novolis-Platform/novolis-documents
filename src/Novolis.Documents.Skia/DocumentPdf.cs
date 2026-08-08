using Novolis.Documents;
using Novolis.Documents.Layout;
using Novolis.Math.Measure;
using SkiaSharp;

namespace Novolis.Documents.Skia;

/// <summary>Options for PDF generation.</summary>
public sealed class DocumentPdfOptions
{
    /// <summary>Optional path to a body TrueType/OpenType font file.</summary>
    public string? BodyFontPath { get; init; }

    /// <summary>Optional path to a bold font file.</summary>
    public string? BoldFontPath { get; init; }
}

/// <summary>Writes a <see cref="PagedDocument"/> to PDF via SkiaSharp.</summary>
public static class DocumentPdf
{
    /// <summary>Writes a PDF file.</summary>
    public static void Write(PagedDocument document, string path, DocumentPdfOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var bytes = ToBytes(document, options);
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllBytes(path, bytes);
    }

    /// <summary>Generates PDF bytes.</summary>
    public static byte[] ToBytes(PagedDocument document, DocumentPdfOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        options ??= new DocumentPdfOptions();

        using var typeface = LoadTypeface(options.BodyFontPath, bold: false);
        using var boldTypeface = LoadTypeface(options.BoldFontPath ?? options.BodyFontPath, bold: true);
        var measurer = new SkiaTextMeasurer(typeface, boldTypeface);
        var plan = DocumentPaginator.Paginate(document, measurer);

        using var stream = new MemoryStream();
        using (var pdf = SKDocument.CreatePdf(stream))
        {
            ArgumentNullException.ThrowIfNull(pdf);
            var width = document.Setup.Trim.Width.Points;
            var height = document.Setup.Trim.Height.Points;

            foreach (var page in plan.Pages)
            {
                using var canvas = pdf.BeginPage(width, height);
                DrawPage(canvas, document, page, typeface, boldTypeface, measurer);
                pdf.EndPage();
            }

            pdf.Close();
        }

        return stream.ToArray();
    }

    static void DrawPage(
        SKCanvas canvas,
        PagedDocument document,
        PageSlice page,
        SKTypeface typeface,
        SKTypeface boldTypeface,
        SkiaTextMeasurer measurer)
    {
        canvas.Clear(SKColors.White);
        var margin = document.Setup.Margin;
        var contentX = margin.Left.Points;
        var contentTop = margin.Top.Points + document.Setup.HeaderBand.Points;
        var contentWidth = document.Setup.Trim.Width.Points - margin.Horizontal.Points;
        var pageWidth = document.Setup.Trim.Width.Points;
        var pageHeight = document.Setup.Trim.Height.Points;

        if (page.Kind == PageKind.Cover)
        {
            DrawCover(canvas, document, typeface, boldTypeface, pageWidth, pageHeight);
            return;
        }

        if (page.ShowHeader && document.Header is { } header)
        {
            var text = FormatChrome(header.Template, document, page.Number);
            DrawCenteredText(canvas, text, typeface, header.FontSizePt,
                contentX, margin.Top.Points, contentWidth, document.Setup.HeaderBand.Points);
        }

        foreach (var placed in page.Blocks)
        {
            var y = contentTop + placed.YInContentPt;
            DrawBlock(canvas, document, placed.Block, typeface, boldTypeface, measurer,
                contentX, y, contentWidth);
        }

        if (page.ShowFooter && document.Footer is { } footer)
        {
            var text = FormatChrome(footer.Template, document, page.Number);
            var footerY = pageHeight - margin.Bottom.Points - document.Setup.FooterBand.Points;
            DrawCenteredText(canvas, text, typeface, footer.FontSizePt,
                contentX, footerY, contentWidth, document.Setup.FooterBand.Points);
        }
    }

    static void DrawCover(
        SKCanvas canvas,
        PagedDocument document,
        SKTypeface typeface,
        SKTypeface boldTypeface,
        float pageWidth,
        float pageHeight)
    {
        var meta = document.Meta;
        float y = pageHeight * 0.35f;
        DrawCenteredText(canvas, meta.Title, boldTypeface, 22f, 40, y, pageWidth - 80, 40);
        y += 36;
        if (!string.IsNullOrWhiteSpace(meta.Subtitle))
        {
            DrawCenteredText(canvas, meta.Subtitle, typeface, 13f, 40, y, pageWidth - 80, 24);
            y += 28;
        }

        if (!string.IsNullOrWhiteSpace(meta.Series))
        {
            DrawCenteredText(canvas, meta.Series, typeface, 12f, 40, y, pageWidth - 80, 22);
            y += 26;
        }

        if (!string.IsNullOrWhiteSpace(meta.Author))
        {
            y += 12;
            DrawCenteredText(canvas, meta.Author, typeface, 11f, 40, y, pageWidth - 80, 20);
        }

        if (!string.IsNullOrWhiteSpace(meta.Rights))
        {
            DrawCenteredText(canvas, meta.Rights, typeface, 8.5f, 40, pageHeight - 80, pageWidth - 80, 18);
        }
    }

    static void DrawBlock(
        SKCanvas canvas,
        PagedDocument document,
        IBlock block,
        SKTypeface typeface,
        SKTypeface boldTypeface,
        SkiaTextMeasurer measurer,
        float x,
        float y,
        float width)
    {
        switch (block)
        {
            case HeadingBlock h:
                var hs = h.Level switch
                {
                    1 => document.Typography.H1SizePt,
                    2 => document.Typography.H2SizePt,
                    _ => document.Typography.H3SizePt,
                };
                DrawWrappedText(canvas, h.Text, boldTypeface, hs, document.Typography.LineHeight, x, y, width);
                break;
            case ParagraphBlock p:
                DrawWrappedText(canvas, p.Text, typeface, document.Typography.BodyFontSizePt,
                    document.Typography.LineHeight, x, y, width);
                break;
            case SceneBreakBlock s:
                DrawCenteredText(canvas, s.Ornament, typeface, document.Typography.SceneBreakSizePt,
                    x, y, width, document.Typography.SceneBreakSizePt * 1.2f);
                break;
            case CoverBlock:
                break;
        }
    }

    static void DrawWrappedText(
        SKCanvas canvas,
        string text,
        SKTypeface typeface,
        float fontSize,
        float lineHeight,
        float x,
        float y,
        float width)
    {
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = SKColors.Black,
        };
        using var font = new SKFont(typeface, fontSize);
        var lines = WrapLines(text, font, width);
        var step = fontSize * lineHeight;
        var yy = y + fontSize;
        foreach (var line in lines)
        {
            canvas.DrawText(line, x, yy, SKTextAlign.Left, font, paint);
            yy += step;
        }
    }

    static void DrawCenteredText(
        SKCanvas canvas,
        string text,
        SKTypeface typeface,
        float fontSize,
        float x,
        float y,
        float width,
        float bandHeight)
    {
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = SKColors.Black,
        };
        using var font = new SKFont(typeface, fontSize);
        var baseline = y + bandHeight * 0.7f;
        canvas.DrawText(text, x + width / 2f, baseline, SKTextAlign.Center, font, paint);
    }

    internal static List<string> WrapLines(string text, SKFont font, float maxWidth)
    {
        var result = new List<string>();
        foreach (var paragraph in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (string.IsNullOrEmpty(paragraph))
            {
                result.Add(string.Empty);
                continue;
            }

            var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var current = string.Empty;
            foreach (var word in words)
            {
                var candidate = current.Length == 0 ? word : current + " " + word;
                if (font.MeasureText(candidate) <= maxWidth || current.Length == 0)
                {
                    current = candidate;
                }
                else
                {
                    result.Add(current);
                    current = word;
                }
            }

            if (current.Length > 0)
                result.Add(current);
        }

        if (result.Count == 0)
            result.Add(string.Empty);
        return result;
    }

    static string FormatChrome(string template, PagedDocument document, int pageNumber) =>
        template
            .Replace("{page}", pageNumber.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{title}", document.Meta.Title, StringComparison.OrdinalIgnoreCase);

    static SKTypeface LoadTypeface(string? path, bool bold)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            var tf = SKTypeface.FromFile(path);
            if (tf is not null)
                return tf;
        }

        return SKTypeface.FromFamilyName(
            "Georgia",
            bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
            SKFontStyleWidth.Normal,
            SKFontStyleSlant.Upright) ?? SKTypeface.Default;
    }
}
