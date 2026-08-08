# Design

## Position

`novolis-documents` is an **orthogonal island**:

```text
paged document model  →  one-column pagination  →  SkiaSharp PDF
```

It is **not** on the Math → Physics → Simulation → Gaming → Avalonia spine (except consuming `Novolis.Math.Measure` for lengths/sizes). No Avalonia, Simulation, Rendering, or Raylib dependencies.

## Packages

```text
Novolis.Math.Measure     (novolis-math)
        ↓
Novolis.Documents          model + fluent builders
        ↓
Novolis.Documents.Layout   DocumentPaginator / PagePlan
        ↓
Novolis.Documents.Skia     DocumentPdf (+ SkiaSharp)
```

| Package | Role |
| --- | --- |
| `Novolis.Documents` | `PagedDocument`, blocks, `Document` / `DocumentBuilder`, trim presets, `Header` / `Footer`, watermark |
| `Novolis.Documents.Layout` | `DocumentPaginator`, `PagePlan`, `ITextMeasurer` |
| `Novolis.Documents.Skia` | `DocumentPdf.Write` / `ToBytes` / `Layout`, embedded Liberation Serif |

Skia types never leak into `Novolis.Documents` or `.Layout` public APIs.

## Page lifecycle

Emitted pages, in order when present:

1. **First** — title / opening page (`FirstPage` + `DocumentMeta`)
2. **Toc** — table of contents from level-1 / `Chapter` titles
3. **Body** — main flow (`PagedDocument.Body` blocks)
4. **Last** — closing / colophon (`LastPage`)

Layout kinds: `PageKind.Cover` (First), `Toc`, `Body`, `Last`.

## Authoring shape (P1)

Preferred construction:

```text
Document.Create → Meta / Page / Watermark → Body { First, Content, Last } → Build
```

- **Body** is the spine name; **Content** is the main block stream inside it.
- **Toc** means the contents page (never call the main flow “Contents”).
- **Chapter** = level-1 heading; forces a page break when prior content exists.
- **Header** / **Footer** are plain type names (no “Running…” / “Chrome…” prefixes).

See [authoring.md](authoring.md).

## Hard non-goals (v1)

- Constraint layout / QuestPDF-style Column–Row positioning engine
- Nested blocks inside table cells
- Footnotes / endnotes
- Markdown/Markdig inside this repo (that lives in markup → documents bridges)
- Competing as a general-purpose PDF SDK
- Domain-specific product vocabulary in the public API (no book/manuscript/fiction types)

## Goals (v1)

- Small, predictable one-column flow suitable for trade paperbacks, reports, and invoices
- Fluent construction that mirrors the page spine
- Immutable `PagedDocument` suitable for mappers and codegen
- Compact PDFs (Latin-subset Liberation Serif by default)
- Domain-agnostic names so manuscripts, invoices, and labs share one model

## Consumers

| Consumer | How |
| --- | --- |
| Apps / dogfood | `Document.Create` + `DocumentPdf` |
| Markup | Map Markdown AST → `PagedDocument` (external package) |
| UBL / XSD experiments | Hand-map Lean invoice → blocks (tests only today) |
| Future manuscript exporters | Map into `PagedDocument`; keep domain terms out of this repo |

## Related governance

- [library-boundaries.md](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/library-boundaries.md)
- [nuget-only-policy.md](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/nuget-only-policy.md)
- [documentation-policy.md](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/documentation-policy.md)
