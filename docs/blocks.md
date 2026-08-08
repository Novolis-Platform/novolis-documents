# Blocks

`IBlock` instances live in `PagedDocument.Body`, nested chapter builders, column slots, and `LastPage.Blocks`. Layout walks them top-to-bottom in a single content column (except `ColumnsBlock`, which splits one row).

## Catalog

| Type | Purpose |
| --- | --- |
| `HeadingBlock` | Level 1–3 titles |
| `ParagraphBlock` | Body prose |
| `TableBlock` | Grid of string cells |
| `ColumnsBlock` | Side-by-side block streams |
| `ImageBlock` | Raster/SVG bytes or path |
| `TextBoxBlock` | Bordered text panel (notes / panels); splits by line across pages |
| `CodeBlock` | Monospace filled panel; splits by line across pages |
| `SceneBreakBlock` | Ornamental break between scenes |
| `LineBreakBlock` | Forced blank body line |
| `PageBreakBlock` | Force a new page in the current region (Body / First / Last) |
| `BlankPageBlock` | Emit an intentionally empty page |

Fluent verbs live on `DocumentContentBuilder` / nested builders — see [authoring.md](authoring.md).

## HeadingBlock

```csharp
new HeadingBlock { Level = 1, Text = "Arrival" }
```

| Level | Fluent | Layout |
| --- | --- | --- |
| 1 | `Chapter` / `H1` | Page break when prior content exists; TOC entry; chapter-title header source |
| 2 | `H2` | Continues flow |
| 3 | `H3` | Continues flow |

Prefer `Chapter` for level 1 so authoring and TOC semantics stay obvious.

## ParagraphBlock

```csharp
new ParagraphBlock { Text = "The river ran cold." }
```

Plain text only. No inline markup in v1.

## TableBlock

```csharp
new TableBlock
{
    Headers = ["#", "Item", "Amount"],
    Rows =
    [
        ["1", "Widget", "10,00"],
        ["2", "Gadget", "20,00"],
    ],
    ColumnWidths = [0.1f, 0.6f, 0.3f],
    ColumnAlignments = [CellAlign.Left, CellAlign.Left, CellAlign.Right],
    Rules = TableRuleStyle.Horizontal,
    HeaderBackground = true,
    RepeatHeaderOnPageBreak = true,
}
```

| Property | Meaning |
| --- | --- |
| `Headers` | Optional header row strings |
| `ShowHeader` | When true (default) and headers exist, paint the header row |
| `Rows` | `IReadOnlyList<IReadOnlyList<string>>` |
| `ColumnWidths` | Fractions summing ~1.0; omit for equal columns |
| `ColumnAlignments` | Per-column `CellAlign` (`Left` / `Center` / `Right`) |
| `Rules` | `None`, `Horizontal`, `Grid` |
| `HeaderBackground` | Light fill behind header row |
| `RepeatHeaderOnPageBreak` | Re-draw header when the table spans pages |

**Page breaks:** Layout splits tables between rows when they no longer fit the content box. Continuation slices keep column widths/alignments/rules. With `RepeatHeaderOnPageBreak` (default **true**), each continuation redraws the header row. Rows are never split mid-cell.

**Non-goal:** nested blocks inside cells. Cells are strings.

## ColumnsBlock

```csharp
new ColumnsBlock
{
    Gap = Length.FromPoints(16),
    Fractions = [0.5f, 0.5f],
    Columns =
    [
        [new HeadingBlock { Level = 3, Text = "From" }, new ParagraphBlock { Text = "…" }],
        [new HeadingBlock { Level = 3, Text = "To" }, new ParagraphBlock { Text = "…" }],
    ],
}
```

Used for invoice party rows, side-by-side payment/summary, etc. Columns share one vertical band; tallest column wins height.

## ImageBlock

```csharp
new ImageBlock
{
    Path = @"C:\path\logo.svg",   // or Bytes = […]
    Width = Length.FromPoints(120),
    Height = Length.FromPoints(40),
}
```

Skia paints rasters and SVG (via Svg.Skia). Prefer explicit size so layout can reserve space before decode.

## TextBoxBlock

Bordered panel of plain lines. Domain-agnostic (callouts, notes, sidebars — consumers supply the lines).

```csharp
new TextBoxBlock
{
    Lines = ["2497.110 17:40", "System Y982283", "Earth Fleet battlecruiser, Quartermaster's office"],
    PaddingPt = 6f,
    BorderStrokePt = 0.8f,
    BorderColor = DocumentColor.Gray,
    Background = DocumentColor.LightGray,
    FontSizePt = 8.5f,
    LineHeight = 1.22f,
    LineGapPt = 1.5f,
    TextColor = DocumentColor.Gray,
}
```

Fluent: `.TextBox(t => t.Lines("…").Padding(6f).Border(0.8f).Background(DocumentColor.LightGray).Font(8.5f))`.

Layout splits by line when the box no longer fits the page (each slice keeps the same border/fill options).

## SceneBreakBlock / PageBreakBlock / BlankPageBlock

| Block | Behavior |
| --- | --- |
| `SceneBreakBlock` | Small ornamental gap (centered dots/line depending on paint) |
| `PageBreakBlock` | Force next content onto a new body page |
| `BlankPageBlock` | Intentionally empty body page (still receives header/footer per includes) |

## Choosing blocks for invoices vs books

Same block types, different composition:

- **Trade sample:** First → Toc → Chapters → Last  
- **Invoice:** often First omitted; Content with logo image, columns (seller/buyer), table (lines), columns (payment/totals) — layout language stays domain-agnostic
