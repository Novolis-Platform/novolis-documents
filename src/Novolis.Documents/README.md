<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-documents">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Documents

Paged document model for Novolis: trim presets, typography, header/footer, watermark, and a closed set of content blocks. No Skia, no Markdown — map sources into `PagedDocument`, then paginate with `Novolis.Documents.Layout` and paint with `Novolis.Documents.Skia`.

## Install

```bash
dotnet add package Novolis.Documents
```

Requires .NET 10 (`net10.0`). Restore from nuget.org + GitHub Packages (`https://nuget.pkg.github.com/Novolis-Platform/index.json`).

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
        .Header(h => h.Template("{title}").IncludeBody().UseChapterTitle())
        .Footer(f => f
            .Template("{page} / {pages}")
            .IncludeFirstPage()
            .IncludeToc()
            .IncludeBody()
            .IncludeLastPage()))
    .Watermark(w => w.Text("DRAFT").Color(DocumentColor.Red).Opacity(0.12f))
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

`Body` is the spine: **First → Content → Last**. `Chapter` is a level-1 heading (page break when prior content exists). Object initializers on `PagedDocument` remain supported for mappers.

`DefaultMargin` is print-oriented (binding 0.75″ / outer 0.5″ / head 0.5″ / foot 0.65″). Use `TrimPresets.ReportMargin` for uniform 1″.

## Docs

| Doc | Topic |
| --- | --- |
| [getting-started](https://github.com/Novolis-Platform/novolis-documents/blob/main/docs/getting-started.md) | Install + first PDF |
| [authoring](https://github.com/Novolis-Platform/novolis-documents/blob/main/docs/authoring.md) | Fluent DSL reference |
| [model](https://github.com/Novolis-Platform/novolis-documents/blob/main/docs/model.md) | `PagedDocument` |
| [blocks](https://github.com/Novolis-Platform/novolis-documents/blob/main/docs/blocks.md) | Block catalog |
| [header-footer](https://github.com/Novolis-Platform/novolis-documents/blob/main/docs/header-footer.md) | Header / footer / watermark |
| [mappers](https://github.com/Novolis-Platform/novolis-documents/blob/main/docs/mappers.md) | Mapping pipelines |

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Documents.Layout` | Paginate into a `PagePlan` |
| `Novolis.Documents.Skia` | Write PDF via SkiaSharp |
| `Novolis.Math.Measure` | Length/Size/Thickness/Rect |

## Support

- Docs: [novolis-documents](https://github.com/Novolis-Platform/novolis-documents)
- Issues: [GitHub Issues](https://github.com/Novolis-Platform/novolis-documents/issues)
