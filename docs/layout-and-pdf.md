# Layout and PDF

## Pipeline

```text
PagedDocument
    → DocumentPaginator.Paginate(document, ITextMeasurer)
    → PagePlan
    → DocumentPdf paint (Skia)
```

`DocumentPdf.Write` / `ToBytes` call pagination with the same Skia measurer used for painting, so wrap heights match ink.

## DocumentPaginator

```csharp
using Novolis.Documents.Layout;

PagePlan plan = DocumentPaginator.Paginate(document, measurer);
```

### Responsibilities

- Emit First / Toc / Body / Last page slices when configured
- Measure and place blocks in a single content column
- Split overflow across body pages
- Apply H1 / `Chapter` page breaks
- Resolve header/footer visibility from include flags
- Track `ChapterTitle` for body pages
- Build `TocEntries` with resolved page numbers when `IncludeToc` is set

### ITextMeasurer

Layout never references Skia. Tests can fake:

```csharp
public interface ITextMeasurer
{
    float MeasureHeight(string text, float widthPt, TextStyle style);
    IReadOnlyList<string> WrapLines(string text, float widthPt, TextStyle style);
}
```

`Novolis.Documents.Skia` supplies `SkiaTextMeasurer` (internal to the PDF path; use `DocumentPdf.Layout` for a plan with real fonts).

## PagePlan

| Type | Role |
| --- | --- |
| `PagePlan` | Ordered `Pages` + `TocEntries` |
| `PageSlice` | One finished page |
| `PlacedBlock` | Block + `YInContentPt` + `HeightPt` |
| `PageKind` | `Cover`, `Toc`, `Body`, `Last` |

`PageSlice` fields of interest:

- `Number` — 1-based
- `ShowHeader` / `ShowFooter` — resolved for that kind
- `ChapterTitle` — for chapter-style headers
- `Blocks` — empty on First (cover is painted from Meta/First, not placed blocks)

## DocumentPdf

```csharp
using Novolis.Documents.Skia;

DocumentPdf.Write(document, @"C:\Users\frank\.novolis\artifacts\out.pdf");
byte[] bytes = DocumentPdf.ToBytes(document);
PagePlan plan = DocumentPdf.Layout(document);
```

### Options

```csharp
var options = new DocumentPdfOptions
{
    BodyFontPath = @"C:\fonts\MySerif.ttf",
    BoldFontPath = @"C:\fonts\MySerif-Bold.ttf",
};
DocumentPdf.Write(document, path, options);
```

Default: embedded Latin-subset **Liberation Serif** (OFL) for compact PDFs. Override paths when you need full Unicode or a house face.

### Paint order (per page)

1. Clear white  
2. Watermark (if included for that kind)  
3. Cover art / placed blocks  
4. Header band text  
5. Footer band text  

### What Skia paints that Layout does not invent

- Actual glyphs, table rules, header background fills  
- SVG/raster images (`ImageBlock`)  
- Scene-break ornament  
- Diagonal watermark  

Layout still owns **geometry**: where blocks sit and which pages exist.

## Content box

```text
┌──────────────── trim ────────────────┐
│  margin.top                          │
│  ┌──── header band ────┐             │
│  │                     │             │
│  ├──── content box ────┤             │
│  │  placed blocks      │             │
│  ├──── footer band ────┤             │
│  │                     │             │
│  margin.bottom                       │
└──────────────────────────────────────┘
```

Content width = `Trim.Width − Margin.Horizontal`.  
Content height = `Trim.Height − Margin.Vertical − HeaderBand − FooterBand`.

## Performance notes

- Prefer the embedded subset for short invoices/samples.  
- Large images dominate file size — keep logos small.  
- Tables with many rows paginate; headers can repeat via `RepeatHeaderOnPageBreak`.
