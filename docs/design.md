# Design

## Position

`novolis-documents` is an **orthogonal island**:

```text
semantic sections  →  renderer-independent pagination  →  SkiaSharp PDF
```

It is **not** on the Math → Physics → Simulation → Gaming → Avalonia spine (except consuming `Novolis.Math.Measure` for lengths/sizes). No Avalonia, Simulation, Rendering, or Raylib dependencies.

## Packages

```text
Novolis.Math.Measure     (novolis-math)
        ↓
Novolis.Documents          semantic model + fluent builders
        ↓
Novolis.Documents.Layout   SectionPaginator / PagePlan
        ↓
Novolis.Documents.Skia     DocumentPdf (+ SkiaSharp)
```

| Package | Role |
| --- | --- |
| `Novolis.Documents` | `Document`, sections, content, compatibility blocks, trim presets, `Header` / `Footer`, watermark |
| `Novolis.Documents.Layout` | `SectionPaginator`, `PagePlan`, semantic fragments, `ITextMeasurer` |
| `Novolis.Documents.Skia` | `DocumentPdf.Write` / `ToBytes` / `Layout`, embedded Liberation Serif |

Skia types never leak into `Novolis.Documents` or `.Layout` public APIs.

## Page lifecycle

Emitted pages, in order when present:

1. **First** — title / opening page (`FirstPage` + `DocumentMeta`)
2. **Toc** — table of contents from level-1 / `Chapter` titles
3. **Body** — main flow (`Document.Sections`)
4. **Last** — closing / colophon (`LastPage`)

`PageKind.Cover` remains a compatibility alias for `PageKind.First`. A contents list is represented by `PagePlan.TocEntries`; it does not create a separate page role.

## Authoring shape (P1)

Preferred construction:

```text
Document.Create → Meta / Page / Watermark → Body { First, Content, Last } → Build
```

- **Sections** is the document spine. `Body` / `Content` remain migration surfaces for older block callers.
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
- Immutable `Document` suitable for mappers and codegen
- Compact PDFs (Latin-subset Liberation Serif by default)
- Domain-agnostic names so manuscripts, invoices, and labs share one model

## Consumers

| Consumer | How |
| --- | --- |
| Apps / dogfood | `Document.Create` + `DocumentPdf` |
| Markup | Map Markdown AST → `Document` (external package) |
| UBL / XSD experiments | Hand-map Lean invoice → sections (tests only today) |
| Future manuscript exporters | Map into `Document`; keep domain terms out of this repo |

## Related governance

- [library-boundaries.md](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/library-boundaries.md)
- [nuget-only-policy.md](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/nuget-only-policy.md)
- [documentation-policy.md](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/documentation-policy.md)
