# Authoring DSL

Entry point: `Document.Create` / `Document.Create(string title)` → `DocumentBuilder` → `Build()` → `PagedDocument`.

This is a **fluent construction** API over the immutable block model. It is not a constraint layout engine.

## Skeleton

```csharp
var document = Document.Create("Sample")
    .Meta(m => m.Author("Example"))
    .Page(p => p.Trade6x9().Header(…).Footer(…))
    .Watermark(w => w.Text("DRAFT").Color(DocumentColor.Red))   // optional
    .Body(b => b
        .First(f => f.Lines("…"))
        .Content(c => c
            .Toc()
            .Chapter("One", ch => ch.Paragraph("…")))
        .Last(l => l.Title("Colophon").Lines("End.")))
    .Build();
```

## DocumentBuilder surface

| Method | Purpose |
| --- | --- |
| `Title` / `Meta` | Document metadata (`DocumentMetaBuilder`) |
| `Page` | Trim, margins, header, footer (`DocumentPageBuilder`) |
| `Typography` | Font sizes / spacing (`TypographyBuilder`) |
| `Watermark` / `NoWatermark` | Diagonal text mark |
| `Body` | Spine: First → Content → Last |
| `Build` | Materialize `PagedDocument` (requires a title) |

## Meta

```csharp
.Meta(m => m
    .Title("…")          // usually from Create(title)
    .Subtitle("…")
    .Series("…")
    .Author("…")
    .Contributors("…")
    .Publisher("…")
    .Subject("…")
    .Description("…")
    .Keywords("a", "b")
    .Identifier("ISBN…")
    .Language("en")
    .Version("1.0")
    .Date(new DateOnly(2026, 8, 8))
    .Rights("© …"))
```

Meta fields feed the First page and `{…}` template placeholders (see [header-footer.md](header-footer.md)).

## Page

```csharp
.Page(p => p
    .Trade6x9()          // or .A4(), .TrimSize(…), .Margins(…)
    .Bands(16f, 16f)     // header / footer band heights (points)
    .Header(h => h.Template("{title}").IncludeBody().UseChapterTitle())
    .Footer(f => f.Template("{page} / {pages}").IncludeBody()))
```

Defaults for both header and footer: **Body only** (First / Toc / Last off). Opt in with `IncludeFirstPage()`, `IncludeToc()`, `IncludeLastPage()`.

Shortcuts:

```csharp
.Header("{title}")                 // body only
.Footer("{page} / {pages}")        // body only
```

## Body spine

`DocumentBodyBuilder`:

| Method | Maps to |
| --- | --- |
| `First(…)` | `FirstPage` + `IncludeCover = true` |
| `Content(…)` | `PagedDocument.Body` blocks + optional Toc flag |
| `Last(…)` | `LastPage` |

### First

Usually one page. When title lines / blocks do not fit, layout continues onto further First pages automatically (no flag).

```csharp
.First(f => f
    .Title("Override")       // optional; else Meta.Title
    .Subtitle("…")
    .Lines("Line A", "Line B")
    .Blocks(b => b.Paragraph("…").PageBreak().Paragraph("…")))
```

### Content

`DocumentContentBuilder` verbs:

| Verb | Block |
| --- | --- |
| `Toc()` | Sets `IncludeToc` (contents page before body flow) |
| `Chapter(title)` / `Chapter(title, ch => …)` | Level-1 heading (+ nested blocks) |
| `H1` / `H2` / `H3` | `HeadingBlock` (prefer `Chapter` for level 1) |
| `Paragraph` | `ParagraphBlock` (`\n` = soft line breaks inside the paragraph) |
| `Table` | `TableBlock` via `TableBuilder` |
| `Columns` | `ColumnsBlock` via `ColumnsBuilder` |
| `Image(path\|bytes, w, h)` | `ImageBlock` |
| `TextBox` | `TextBoxBlock` via `TextBoxBuilder` (border / fill / type options) |
| `SceneBreak` | `SceneBreakBlock` |
| `LineBreak` | Forced blank body line |
| `PageBreak` / `BlankPage` | Explicit page breaks |
| `Add` / `AddRange` | Arbitrary `IBlock` |

### Chapter

```csharp
.Chapter("Arrival")                          // H1 only
.Chapter("Arrival", ch => ch
    .Paragraph("…")
    .H2("Quay-side")
    .Table(t => t.Headers("A", "B").Row("1", "2")))
```

Semantics: emit `HeadingBlock { Level = 1 }`. Layout starts a new page when prior content exists. With `Header.UseChapterTitle()`, subsequent body pages carry that title in the header.

### Last

Usually one page. Overflow continues onto further Last pages automatically (no flag).

```csharp
.Last(l => l
    .Title("Colophon")
    .Lines("End of sample.")
    .Blocks(b => b
        .Table(t => t.Headers("K", "V").Row("Engine", "Skia"))
        .PageBreak()
        .Paragraph("Continued colophon.")))
```

## Tables

```csharp
.Table(t => t
    .Headers("#", "Item", "Amount")
    .Row("1", "Widget", "10,00")
    .ColumnWidths(0.1f, 0.6f, 0.3f)
    .Align(CellAlign.Left, CellAlign.Left, CellAlign.Right)
    .Rules(TableRuleStyle.Horizontal)
    .ShowHeader()
    .HeaderBackground()
    .RepeatHeaderOnPageBreak())   // default true — header repeats on each page slice
```

Bulk rows: `.Rows(enumerableOfStringLists)`.

See [blocks.md](blocks.md#tableblock).

## Columns

```csharp
.Columns(c => c
    .Gap(16f)
    .Fractions(0.5f, 0.5f)
    .Column(left => left.H3("From").Paragraph("…"))
    .Column(right => right.H3("To").Paragraph("…")))
```

## Typography

```csharp
.Typography(t => t
    .BodySize(10f)
    .HeadingSizes(16f, 12f, 10f)
    .TableSize(9f)
    .LineHeight(1.22f)
    .ParagraphSpacing(4f)
    .AfterHeading(8f, 4f)
    .TableCells(3f, 0.4f))
```

## Watermark

```csharp
.Watermark(w => w
    .Text("DRAFT")
    .Color(DocumentColor.Red)    // named colors; default is Red
    .Opacity(0.12f)
    .FontSize(54f)
    .Rotation(-32f)
    .On(WatermarkPages.All))
```

## Object initializers

Everything the DSL builds is ordinary immutable types. Skip the builder when mapping:

```csharp
new PagedDocument { Meta = …, Setup = …, Typography = …, Body = […] }
```

See [mappers.md](mappers.md).
