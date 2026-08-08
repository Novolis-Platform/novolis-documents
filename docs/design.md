# Design

## Position

`novolis-documents` is an **orthogonal island**: book PDF model → pagination → Skia paint. It is not on the Math→Physics→Simulation spine (except consuming `Novolis.Math.Measure`). No Avalonia, Simulation, Rendering, or Raylib dependencies.

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
| `Novolis.Documents` | `BookDocument`, blocks, trim presets, chrome |
| `Novolis.Documents.Layout` | `BookPaginator`, `PagePlan`, `ITextMeasurer` |
| `Novolis.Documents.Skia` | `BookPdf.Write` / `ToBytes` |

## Hard non-goals (v1)

- Constraint layout / fluent Column-Row DSL (QuestPDF)
- Tables, images, lists, code blocks, footnotes, multi-column
- Markdown/Markdig inside this repo
- Competing as a general-purpose PDF SDK

## Chrome rules

- Cover: no running header/footer
- TOC: footer page numbers only
- Body: header + footer; optional suppress header on chapter-open pages
- Last: optional colophon

## Manuscript

Books-grade QuestPDF remains the default Manuscript exporter until this island proves acceptance on real book trees. No Manuscript adapter in v1.
