<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-documents/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-documents/) · [Source](https://github.com/Novolis-Platform/novolis-documents)
<!-- novolis-pkg-brand:end -->

# Novolis.Documents

Semantic document model for Novolis: trim presets, typography, header/footer, watermark,
sections, and semantic content. No Skia, no Markdown — map sources into `Document`, then
paginate with `Novolis.Documents.Layout` and paint with `Novolis.Documents.Skia`.

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
        .Footer(f => f.Template("{page} / {pages}").IncludeBody()))
    .Watermark(w => w.Text("DRAFT").Color(DocumentColor.Red).Opacity(0.12f))
    .FirstPage(page => page.Section(section => section.Text("Trade sample")))
    .Section(section => section
        .Heading("Section One")
        .Text("Once upon a time…"))
    .Table(table => table
        .Header("A", "B")
        .Row("1", "2"))
    .LastPage(page => page.Section(section => section.Text("End.")))
    .Build();
```

The document spine is **First → Sections → Last**. `Body` and block object initializers remain
supported as migration surfaces for older callers.

`DefaultMargin` is print-oriented (binding 0.75″ / outer 0.5″ / head 0.5″ / foot 0.65″). Use `TrimPresets.ReportMargin` for uniform 1″.

## Docs

| Doc | Topic |
| --- | --- |
| [getting-started](https://github.com/Novolis-Platform/novolis-documents/blob/main/docs/getting-started.md) | Install + first PDF |
| [authoring](https://github.com/Novolis-Platform/novolis-documents/blob/main/docs/authoring.md) | Fluent DSL reference |
| [model](https://github.com/Novolis-Platform/novolis-documents/blob/main/docs/model.md) | `Document` and sections |
| [blocks](https://github.com/Novolis-Platform/novolis-documents/blob/main/docs/blocks.md) | Semantic content |
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
