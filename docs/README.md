# Novolis.Documents documentation

Semantic document island for Novolis: immutable sections → layout plan → SkiaSharp PDF.

| Doc | What it covers |
| --- | --- |
| [getting-started.md](getting-started.md) | Install, first PDF, dogfood samples, build/test |
| [design.md](design.md) | Package stack, goals, non-goals, page lifecycle |
| [authoring.md](authoring.md) | Fluent `Document.Create` DSL (P1 spine) |
| [model.md](model.md) | `Document`, semantic sections, meta, page setup, typography |
| [blocks.md](blocks.md) | Compatibility block catalog and semantic equivalents |
| [header-footer.md](header-footer.md) | Header / footer includes, templates, chapter title, watermark |
| [layout-and-pdf.md](layout-and-pdf.md) | `DocumentPaginator`, `PagePlan`, `DocumentPdf` |
| [mappers.md](mappers.md) | Mapping external formats into `Document` |
| [faq.md](faq.md) | Common questions |
| [release.md](release.md) | CalVer / publish |

## Packages

| Package | Role |
| --- | --- |
| [`Novolis.Documents`](../src/Novolis.Documents/README.md) | Model + fluent builders |
| [`Novolis.Documents.Layout`](../src/Novolis.Documents.Layout/README.md) | Pagination |
| [`Novolis.Documents.Skia`](../src/Novolis.Documents.Skia/README.md) | PDF paint |

## Mental model

```text
Document.Create(title)
  .Meta(…)
  .Page(p => p.Header(…).Footer(…))
  .Watermark(…)          // optional
  .FirstPage(p => p.Section(…))
  .Section(s => s.Heading(…).Text(…))
  .Table(t => t.Header(…).Row(…))
  .LastPage(p => p.Section(…))
  .Build()
        │
        ▼
   Document  (immutable)
        │
        ▼
 SectionPaginator.Paginate  →  PagePlan
        │
        ▼
 DocumentPdf.Write / ToBytes  →  PDF
```
