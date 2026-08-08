# Design

## Position

`novolis-documents` is an **orthogonal island**: paged document model → pagination → Skia paint. It is not on the Math→Physics→Simulation spine (except consuming `Novolis.Math.Measure`). No Avalonia, Simulation, Rendering, or Raylib dependencies.

## Packages

```text
Novolis.Math.Measure     (novolis-math)
        ↓
Novolis.Documents
        ↓
Novolis.Documents.Layout
        ↓
Novolis.Documents.Skia   (+ SkiaSharp)
```

| Package | Role |
|---------|------|
| `Novolis.Documents` | `PagedDocument`, blocks, `DocumentBuilder` DSL, trim presets, chrome |
| `Novolis.Documents.Layout` | `DocumentPaginator`, `PagePlan`, `ITextMeasurer` |
| `Novolis.Documents.Skia` | `DocumentPdf.Write` / `ToBytes` |

## Authoring DSL

`Document.Create` → `Meta` / `Page` → `Body { First, Content, Last }` is the fluent construction API over the immutable block model. `Chapter` = level-1 heading (page break when needed). `Toc` is the contents page. Header/Footer are plain names on `Page`. Not a constraint solver.

## Hard non-goals (v1)

- Constraint layout / QuestPDF-style Column-Row positioning engine
- Nested blocks in table cells, footnotes
- Markdown/Markdig inside this repo
- Competing as a general-purpose PDF SDK
- Domain-specific product vocabulary in the public API (no book/manuscript/fiction types)

## Chrome rules

Defaults via `ChromeOptions` (overridable on `Page.Chrome(…)`):

- First: footer only (page numbers)
- Toc: footer only
- Body: header + footer; optional suppress header on level-1-open pages
- Last: footer only

`Watermark` is optional diagonal text behind content (`WatermarkPages` selects regions).

## Consumers

Higher-level product exporters (e.g. manuscript) may map into `PagedDocument`. This repo stays domain-agnostic.
