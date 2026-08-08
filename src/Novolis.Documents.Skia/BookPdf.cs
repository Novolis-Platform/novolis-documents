using Novolis.Documents;
using Novolis.Documents.Layout;
using Novolis.Math.Measure;
using SkiaSharp;

namespace Novolis.Documents.Skia;

/// <summary>Options for PDF generation.</summary>
public sealed class BookPdfOptions
{
    /// <summary>Optional path to a body TrueType/OpenType font file.</summary>
    public string? BodyFontPath { get; init; }

    /// <summary>Optional path to a bold font file.</summary>
    public string? BoldFontPath { get; init; }
}

/// <summary>Writes a <see cref="BookDocument"/> to PDF via SkiaSharp.</summary>
public static class BookPdf
{
    /// <summary>Writes a PDF file.</summary>
    public static void Write(BookDocument book, string path, BookPdfOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var bytes = ToBytes(book, options);
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllBytes(path, bytes);
    }

    /// <summary>Generates PDF bytes.</summary>
    public static byte[] ToBytes(BookDocument book, BookPdfOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(book);
        options ??= new BookPdfOptions();

        using var typeface = LoadTypeface(options.BodyFontPath, bold: false);
        using var boldTypeface = LoadTypeface(options.BoldFontPath ?? options.BodyFontPath, bold: true);
        var measurer = new SkiaTextMeasurer(typeface, boldTypeface);
        var plan = BookPaginator.Paginate(book, measurer);

        using var stream = new MemoryStream();
        using (var document = SKDocument.CreatePdf(stream))
        {
            ArgumentNullException.ThrowIfNull(document);
            var width = book.Setup.Trim.Width.Points;
            var height = book.Setup.Trim.Height.Points;

            foreach (var page in plan.Pages)
            {
                using var canvas = document.BeginPage(width, height);
                DrawPage(canvas, book, page, typeface, boldTypeface, measurer);
                document.EndPage();
            }

            document.Close();
        }

        return stream.ToArray();
    }

    static void DrawPage(
        SKCanvas canvas,
        BookDocument book,
        PageSlice page,
        SKTypeface typeface,
        SKTypeface boldTypeface,
        SkiaTextMeasurer measurer)
    {
        canvas.Clear(SKColors.White);
        var margin = book.Setup.Margin;
        var contentX = margin.Left.Points;
        var contentTop = margin.Top.Points + book.Setup.HeaderBand.Points;
        var contentWidth = book.Setup.Trim.Width.Points - margin.Horizontal.Points;
        var pageWidth = book.Setup.Trim.Width.Points;
        var pageHeight = book.Setup.Trim.Height.Points;

        if (page.Kind == PageKind.Cover)
        {
            DrawCover(canvas, book, typeface, boldTypeface, pageWidth, pageHeight);
            return;
        }

        if (page.ShowHeader && book.Header is { } header)
        {
            var text = FormatChrome(header.Template, book, page.Number);
            DrawCenteredText(canvas, text, typeface, header.FontSizePt,
                contentX, margin.Top.Points, contentWidth, book.Setup.HeaderBand.Points);
        }

        foreach (var placed in page.Blocks)
        {
            var y = contentTop + placed.YInContentPt;
            DrawBlock(canvas, book, placed.Block, typeface, boldTypeface, measurer,
                contentX, y, contentWidth);
        }

        if (page.ShowFooter && book.Footer is { } footer)
        {
            var text = FormatChrome(footer.Template, book, page.Number);
            var footerY = pageHeight - margin.Bottom.Points - book.Setup.FooterBand.Points;
            DrawCenteredText(canvas, text, typeface, footer.FontSizePt,
                contentX, footerY, contentWidth, book.Setup.FooterBand.Points);
        }
    }

    static void DrawCover(
        SKCanvas canvas,
        BookDocument book,
        SKTypeface typeface,
        SKTypeface boldTypeface,
        float pageWidth,
        float pageHeight)
    {
        var meta = book.Meta;
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
        BookDocument book,
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
                    1 => book.Typography.ChapterTitleSizePt,
                    2 => book.Typography.H2SizePt,
                    _ => book.Typography.H3SizePt,
                };
                DrawWrappedText(canvas, h.Text, boldTypeface, hs, book.Typography.LineHeight, x, y, width);
                break;
            case ParagraphBlock p:
                DrawWrappedText(canvas, p.Text, typeface, book.Typography.BodyFontSizePt,
                    book.Typography.LineHeight, x, y, width);
                break;
            case SceneBreakBlock s:
                DrawCenteredText(canvas, s.Ornament, typeface, book.Typography.SceneBreakSizePt,
                    x, y, width, book.Typography.SceneBreakSizePt * 1.2f);
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

    static string FormatChrome(string template, BookDocument book, int pageNumber) =>
        template
            .Replace("{page}", pageNumber.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{title}", book.Meta.Title, StringComparison.OrdinalIgnoreCase);

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
