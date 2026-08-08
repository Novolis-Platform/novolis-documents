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
| `Novolis.Documents` | `PagedDocument`, blocks, trim presets, chrome |
| `Novolis.Documents.Layout` | `DocumentPaginator`, `PagePlan`, `ITextMeasurer` |
| `Novolis.Documents.Skia` | `DocumentPdf.Write` / `ToBytes` |

## Hard non-goals (v1)

- Constraint layout / fluent Column-Row DSL (QuestPDF)
- Images, nested blocks in cells, footnotes, multi-column
- Markdown/Markdig inside this repo
- Competing as a general-purpose PDF SDK
- Domain-specific product vocabulary in the public API (no book/manuscript/fiction types)

## Chrome rules

- First/title page: no running header/footer (`First` / `IncludeCover`)
- Contents: footer page numbers only
- Body: header + footer; optional suppress header on level-1-open pages
- Last: optional colophon (`LastPage`)

## Consumers

Higher-level product exporters (e.g. manuscript) may map into `PagedDocument`. This repo stays domain-agnostic.
