<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-documents">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Documents

Paged document model for Novolis: trim presets, typography, running chrome, and a closed set of content blocks. No Skia, no Markdown — map sources into `PagedDocument` then paginate with `Novolis.Documents.Layout`.

## Install

```bash
dotnet add package Novolis.Documents
```

## Quick start

```csharp
using Novolis.Documents;

var document = Document.Create("Sample")
    .Meta(m => m
        .Author("Example")
        .Publisher("Novolis")
        .Subject("Demo")
        .Keywords("pdf", "documents")
        .Date(new DateOnly(2026, 8, 8)))
    .Page(p => p
        .Trade6x9()
        .Header("{title}")
        .Footer("{page} / {pages}")
        .Chrome(c => c.PageNumbersOnFrontMatter()))
    .Watermark(w => w.Text("DRAFT").Color("#C02020").Opacity(0.12f))
    .Body(b => b
        .First(f => f.Lines("Trade sample"))
        .Content(c => c
            .Toc()
            .Chapter("Section One", ch => ch
                .Paragraph("Once upon a time…")
                .Table(t => t.Headers("A", "B").Row("1", "2"))))
        .Last(l => l.Title("Colophon").Lines("End.")))
    .Build();
```

`Body` is the spine: **First → Content → Last**. `Chapter` is a level-1 heading (page break when prior content exists). Page numbers on First/Toc/Last are on by default (`ChromeOptions`); use `.Chrome(c => c.QuietFrontMatter())` to turn them off.

`DefaultMargin` is print-oriented (binding 0.75″ / outer 0.5″ / head 0.5″ / foot 0.65″). Use `TrimPresets.ReportMargin` for uniform 1″.
## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Documents.Layout` | Paginate into a `PagePlan` |
| `Novolis.Documents.Skia` | Write PDF via SkiaSharp |
| `Novolis.Math.Measure` | Length/Size/Thickness/Rect |

## Support

- Docs: [novolis-documents](https://github.com/Novolis-Platform/novolis-documents)
- Issues: [GitHub Issues](https://github.com/Novolis-Platform/novolis-documents/issues)
