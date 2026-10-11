# Novolis Documents Section Layout Specification v1.0

## 1. Purpose

`Novolis.Documents` is a semantic paged-document construction and layout library for .NET.

Its primary purpose is to make structured printable documents easy to express without exposing PDF primitives, rendering APIs, HTML, CSS, or low-level coordinate systems.

The fundamental model is:

```text
Document
    ↓
vertical sequence of ISection
    ↓
section-specific layout
    ↓
pagination
    ↓
PagePlan
    ↓
renderer
    ↓
PDF
```

The design is inspired by the general usability of declarative PDF composition libraries such as QuestPDF, but deliberately uses a different semantic model.

The central rule is:

> A document is constructed as a vertical sequence of sections.

A section may internally contain:

- content,
- columns,
- tables,
- images,
- key/value information,
- nested containers,
- or other structured layout,

but the document itself never becomes an arbitrary tree of rows and columns.

This keeps document construction readable as document structure rather than graphical layout instructions.

---

# 2. Scope

Version 1 targets:

- reports,
- invoices,
- statements,
- trade documents,
- structured reference documents,
- books with simple flow,
- generated administrative documents,
- other predominantly vertical paged documents.

The primary feature-completeness target is:

> The library must be capable of sensibly representing the complete Peppol BIS Billing 3.0 business model without renderer-specific escape hatches.

This includes invoice and credit-note presentation.

The broader long-term goal may include arbitrary UBL documents, but arbitrary UBL support is explicitly outside v1.

---

# 3. Non-goals

Version 1 is not:

- an HTML renderer,
- a CSS engine,
- a browser layout engine,
- a general publishing system,
- a desktop publishing application,
- a PDF editing library,
- a PDF parsing library,
- a replacement for arbitrary vector graphics APIs,
- a full constraint-layout engine,
- a generalized QuestPDF clone.

The following are explicitly outside initial scope:

- arbitrary absolute positioning,
- overlapping document content,
- floating objects,
- text wrapping around arbitrary shapes,
- arbitrary nested row/column structures,
- page-spanning table rows,
- arbitrary rowspan,
- complex colspan semantics,
- footnotes,
- endnotes,
- advanced widow/orphan control,
- HTML input,
- CSS input,
- JavaScript,
- PDF form controls,
- digital signatures,
- PDF editing,
- PDF/A conformance,
- tagged-PDF accessibility structure,
- arbitrary UBL rendering.

Freeform canvas-oriented PDF output remains an appropriate responsibility of the owning library and renderer.

---

# 4. Architectural principles

## 4.1 Semantic structure first

The public model should describe the document's meaning and structure.

Prefer:

```text
InvoiceSummarySection
TableSection
ColumnsSection
Text
Number
```

over:

```text
Row
Column
Row
Container
Canvas
```

as the primary authoring vocabulary.

Geometry is an implementation concern of layout.

---

## 4.2 Vertical document flow

The document root contains only sections:

```csharp
public sealed record Document(
    IReadOnlyList<ISection> Sections);
```

Sections are processed in order.

Normal progression is always downward through the document.

A section may occupy:

- part of a page,
- one whole page,
- multiple pages.

Sections never position sibling sections horizontally.

Horizontal composition exists only inside a section.

---

## 4.3 Orthogonal capabilities

The fundamental contracts are:

```csharp
public interface ISection;

public interface IContent;

public interface IContainer : IContent
{
    IReadOnlyList<IContent> Contents { get; }
}
```

These concepts are orthogonal.

### `ISection`

Participates directly in document flow and pagination.

### `IContent`

May be rendered inside a section or container.

### `IContainer`

Owns nested content.

An object may implement more than one interface where appropriate.

There is no requirement that every section be a container.

For example:

```text
TableSection
    ISection

Column
    IContainer
    IContent

Text
    IContent

Number
    IContent
```

---

# 5. Package boundaries

The existing package separation remains desirable.

```text
Novolis.Documents
    semantic document model
    builders
    section/content types
        ↓
Novolis.Documents.Layout
    measurement
    section layout
    pagination
    PagePlan
        ↓
Novolis.Documents.Skia
    Skia text measurement
    painting
    PDF production
```

Potentially:

```text
Novolis.Markup.Markdown.Documents
    Markdown → Document mapping

Novolis.Manuscript.*
    manuscript → Document mapping

future:
Novolis.Billing.Documents
    BillingDocument → Document mapping
```

The core model must not reference:

- SkiaSharp,
- UBL types,
- Peppol types,
- Markdown types,
- manuscript types,
- PDF parser types.

---

# 6. Document model

The root shape should represent document-wide configuration separately from flow.

Conceptually:

```csharp
public sealed record Document(
    DocumentSetup Setup,
    IReadOnlyList<ISection> Sections,
    PageDefinition? FirstPage = null,
    PageDefinition? LastPage = null,
    Header? Header = null,
    Footer? Footer = null,
    DocumentMetadata? Metadata = null);
```

Collections are never null.

Optional semantic concepts may be null.

---

# 7. Page kinds

The library recognizes three fundamental page roles:

```csharp
public enum PageKind
{
    First,
    Body,
    Last
}
```

The paginator may internally differentiate additional states if necessary, but they should not leak into author-facing semantics without a concrete need.

---

# 8. First page

The first page may differ structurally from normal body pages.

Typical uses:

- invoice title/header,
- report cover,
- book cover,
- account statement identity,
- recipient information,
- document metadata.

The first page may contain a dedicated set of sections.

Conceptually:

```csharp
.FirstPage(page => page
    .Section(...)
    .Section(...))
```

The first page does not need to share the normal body header/footer configuration.

---

# 9. Last page

The final page may also be unique.

Typical uses:

- payment instructions,
- legal text,
- remittance information,
- closing information,
- colophon,
- terms and conditions.

A last page is a true page definition, not simply a footer on the final body page.

Whether the final-page content:

- receives its own page,
- or may occupy the remainder of the final body page,

should be explicit configuration.

Default v1 behavior:

> A configured `LastPage` is a distinct page.

This keeps pagination deterministic.

---

# 10. Sections

A section is the primary flow unit.

Every section owns its pagination semantics.

Likely initial concrete section types:

```text
Section
TextSection
ColumnsSection
TableSection
ImageSection
KeyValueSection
SpacerSection
RuleSection
PageBreakSection
CompositeSection
```

Not all need to exist immediately.

The minimum useful v1 set is:

```text
Section
ColumnsSection
TableSection
PageBreakSection
```

with ordinary content represented underneath them.

---

# 11. General-purpose Section

A normal section contains vertically stacked content.

For example:

```csharp
.Section(section => section
    .Heading("Supplier")
    .Text("Acme AS")
    .Text("Example Street 1")
    .Text("NO-4600 Kristiansand"))
```

Internally:

```text
Section
├── Heading
├── Text
├── Text
└── Text
```

Its contents flow vertically.

If the section may split, content boundaries become legal fragmentation points.

---

# 12. Content

Initial semantic content primitives should remain small.

Recommended:

```text
Text
Number
Image
Heading
Rule
Spacer
KeyValue
```

Additional content should be added only where a real document requirement demonstrates the need.

---

# 13. Text

Text is semantic textual content.

Conceptually:

```csharp
public sealed record Text(
    string Value,
    TextStyle? Style = null)
    : IContent;
```

Text is:

- measurable,
- wrappable,
- splittable by line,
- locale-independent until presentation rules are applied.

Empty text is legal but normally renders nothing.

---

# 14. Number

`Number` remains numeric until rendering.

Do not prematurely convert numeric values into strings.

Conceptually:

```csharp
public sealed record Number(
    decimal Value,
    NumberFormat Format)
    : IContent;
```

Potential number styles include:

```csharp
public enum NumberStyle
{
    Decimal,
    Integer,
    Quantity,
    Percentage,
    Currency
}
```

Currency should be explicitly represented.

For example:

```csharp
new Number(
    1234.56m,
    NumberFormat.Currency("NOK"));
```

This permits:

- locale-aware formatting,
- decimal alignment,
- consistent precision,
- semantic rendering,
- numeric table alignment,
- formatting decisions at presentation time.

This is particularly important for billing documents.

---

# 15. Heading

Heading retains hierarchy.

```csharp
public sealed record Heading(
    string Text,
    int Level)
    : IContent;
```

Typical behavior:

- levels 1–4 supported initially,
- configurable spacing,
- optional `KeepWithNext`,
- optional page break before specific levels.

Heading hierarchy may later contribute to PDF outlines.

---

# 16. Image

Images are content, not layout.

Supported initial forms:

- raster bytes,
- raster path,
- SVG bytes,
- SVG path.

Conceptually:

```csharp
public sealed record Image(
    ImageSource Source,
    ImageSizing Sizing)
    : IContent;
```

Renderer-specific image types must not leak into the semantic API.

---

# 17. Containers

A container owns a collection of content.

Example:

```csharp
public sealed record Column(
    IReadOnlyList<IContent> Contents)
    : IContainer;
```

Containers may establish local flow behavior but do not independently participate in document pagination unless also implemented as sections.

---

# 18. Columns

A `ColumnsSection` arranges columns horizontally inside one vertically flowing section.

Conceptually:

```text
ColumnsSection
├── Column
│   ├── Heading
│   └── Text
└── Column
    ├── Heading
    └── Text
```

The document still sees exactly one section.

Columns should support:

- fixed widths,
- automatic widths,
- proportional weights,
- gaps.

For example:

```csharp
.Columns(columns => columns
    .Column(ColumnWidth.Weight(1), ...)
    .Column(ColumnWidth.Weight(1), ...))
```

Initial width modes:

```csharp
public abstract record ColumnWidth
{
    public sealed record Fixed(float Points) : ColumnWidth;
    public sealed record Auto : ColumnWidth;
    public sealed record Weight(float Value) : ColumnWidth;
}
```

Resolution order:

1. fixed widths,
2. auto widths,
3. remaining width distributed by weight.

Avoid CSS-style width negotiation in v1.

---

# 19. Column pagination

Columns introduce an important policy question.

Initial v1 behavior:

> A `ColumnsSection` is measured as a single section whose height is the height of its tallest column.

Its internal columns flow independently downward within that height.

Columns do not independently continue onto separate pages.

If a columns section does not fit:

- it moves to the next page if `KeepTogether`,
- or it may split only at explicit internal content boundaries supported by the section implementation.

For initial simplicity:

> ColumnsSection should default to `KeepTogether`.

This is suitable for common invoice party/address layouts.

---

# 20. Tables

Tables are first-class sections.

```csharp
public sealed record TableSection(...) : ISection;
```

A table should not initially be expressed as an arbitrary `IContent` hierarchy.

The table owns:

- columns,
- header,
- rows,
- row measurement,
- pagination,
- repeated headers,
- table-level styling.

---

# 21. Table structure

Conceptually:

```text
TableSection
├── Columns[]
├── Header?
├── Rows[]
└── Footer?
```

Initial model:

```csharp
public sealed record TableSection(
    IReadOnlyList<TableColumn> Columns,
    TableRow? Header,
    IReadOnlyList<TableRow> Rows,
    TableOptions Options)
    : ISection;
```

---

# 22. Table columns

A table column defines width and presentation behavior.

```csharp
public sealed record TableColumn(
    ColumnWidth Width,
    HorizontalAlignment Alignment = HorizontalAlignment.Left);
```

Initial width modes are the same semantic concepts as section columns:

- fixed,
- auto,
- weight.

This avoids two independent sizing systems.

---

# 23. Table rows

A row is not itself a section.

```csharp
public sealed record TableRow(
    IReadOnlyList<TableCell> Cells);
```

A row is measured as a whole.

Initial invariant:

> Table rows never split across pages.

This is a major simplification and an intentional v1 limitation.

---

# 24. Table cells

A cell may contain one or more pieces of content.

```csharp
public sealed record TableCell(
    IReadOnlyList<IContent> Contents);
```

Initial cell support should include:

- text,
- number,
- heading-like emphasized text,
- image,
- simple vertical content.

Do not permit arbitrary nested sections inside cells.

---

# 25. Table pagination

A table may span an arbitrary number of pages.

The algorithm is:

```text
place table header
for each row
    measure row

    if row fits
        place row

    otherwise
        start new page
        repeat header
        place row
```

The table header is reproduced automatically whenever the table continues onto another page.

This is mandatory behavior.

---

# 26. Table header rules

The paginator must never render a repeated table header with no data row beneath it.

Therefore:

```text
remaining height
    < header height + minimum row height
```

causes the table continuation to move to the next page.

A header may itself contain wrapped text.

Header height is measured exactly like other table rows.

---

# 27. Oversized table rows

If a row is taller than the entire available body area of an otherwise empty page, v1 should fail explicitly.

Example:

```text
TableRowTooLargeException
```

or an equivalent structured layout failure.

Do not silently clip content.

Do not automatically split table rows in v1.

Future support may introduce row fragmentation deliberately.

---

# 28. Repeating table headers

Header repetition must preserve:

- content,
- styling,
- column widths,
- alignment,
- background,
- borders,
- padding.

The header should be laid out once logically but painted independently on every page fragment.

---

# 29. Table footers

A footer row is useful but not required for the first minimal implementation.

When introduced, two distinct concepts may be needed:

```text
ContinuationFooter
FinalFooter
```

Do not overload one footer with ambiguous behavior.

---

# 30. Table styling

Initial support:

- header background,
- header font emphasis,
- row padding,
- cell alignment,
- horizontal rules,
- vertical rules,
- alternating row styling if justified,
- outer border,
- numeric alignment.

Styling should be defined through semantic style objects rather than renderer types.

---

# 31. Layout options

Sections should support a small common layout policy.

Conceptually:

```csharp
public sealed record SectionLayout(
    bool KeepTogether = false,
    bool KeepWithNext = false,
    float SpaceBefore = 0,
    float SpaceAfter = 0);
```

Avoid building a CSS property universe.

---

# 32. KeepTogether

`KeepTogether` means:

> If the complete section fits on an otherwise empty page but does not fit in the current remaining space, move the entire section to the next page.

If the section is taller than a full page, normal splitting rules apply where the section supports splitting.

For an intrinsically unsplittable oversized section, layout fails explicitly.

---

# 33. KeepWithNext

`KeepWithNext` means:

> The section should not be placed unless enough room exists for both this section and the minimum viable fragment of the following section.

Primary use:

- headings,
- short labels before tables,
- summary headings,
- subsection titles.

It is a hint enforced where deterministic.

---

# 34. Explicit page break

A page break is represented structurally:

```csharp
public sealed record PageBreakSection : ISection;
```

It does not need to implement `IContainer`.

If already at the top of a fresh page, its behavior should be deterministic and configurable.

Default:

> do not emit an additional blank page.

---

# 35. Measurement

Layout must be deterministic.

Every layout-capable type must be measurable using the exact typography metrics that will be used for rendering.

The core measurement abstraction remains renderer-neutral:

```csharp
public interface ITextMeasurer
{
    float MeasureHeight(
        string text,
        float width,
        TextStyle style);

    IReadOnlyList<string> WrapLines(
        string text,
        float width,
        TextStyle style);
}
```

Additional measurement primitives may be introduced when necessary.

---

# 36. Section layout contract

The key layout operation is:

```text
Given:
    section
    available width
    available height

Return:
    fits
    split
    move
    impossible
```

Conceptually:

```csharp
public abstract record SectionLayoutResult
{
    public sealed record Fits(
        SectionFragment Fragment)
        : SectionLayoutResult;

    public sealed record Split(
        SectionFragment Head,
        ISection Tail)
        : SectionLayoutResult;

    public sealed record NeedsNewPage
        : SectionLayoutResult;

    public sealed record Impossible(
        LayoutFailure Failure)
        : SectionLayoutResult;
}
```

Exact naming may differ.

The important design property is that pagination decisions are explicit results rather than hidden mutations.

---

# 37. Layout fragments

The semantic section and the rendered fragment are separate concepts.

A single:

```text
TableSection
```

may become:

```text
TableFragment page 1
TableFragment page 2
TableFragment page 3
```

Likewise a text section may fragment across pages.

The semantic model remains unchanged.

---

# 38. PagePlan

Pagination produces a renderer-independent `PagePlan`.

Conceptually:

```csharp
public sealed record PagePlan(
    IReadOnlyList<PageSlice> Pages);
```

Each page contains positioned fragments.

```csharp
public sealed record PageSlice(
    int Number,
    PageKind Kind,
    IReadOnlyList<PlacedFragment> Fragments);
```

A placed fragment contains geometry and semantic render information.

The renderer must not make pagination decisions.

---

# 39. Pagination algorithm

The core document algorithm remains deliberately simple:

```text
create current page

for each section
    while section remains

        layout against remaining page space

        Fits:
            place fragment
            continue next section

        Split:
            place head
            create next body page
            continue with tail

        NeedsNewPage:
            create next body page
            retry section

        Impossible:
            fail with diagnostic
```

Special handling applies for:

- first page,
- final page,
- header/footer areas,
- explicit page breaks.

---

# 40. Page geometry

Page setup defines:

- trim size,
- margins,
- header area,
- footer area,
- content area.

The layout engine works in physical units converted to points.

Reuse `Novolis.Math.Measure` where appropriate.

Common page presets should include at least:

- A4,
- Letter,
- Trade 6×9.

No page preset may affect semantic layout behavior.

---

# 41. Headers

Headers belong to page chrome, not document flow.

They may reference:

- title,
- section/chapter context,
- page number,
- total pages,
- metadata.

They do not consume section flow space beyond their reserved header band.

Header visibility must be configurable by page kind.

---

# 42. Footers

Footers follow the same rules as headers.

Typical variables:

```text
{page}
{pages}
{title}
{section}
```

No arbitrary template language is required.

---

# 43. Watermarks

Watermarks remain renderer concerns driven by semantic configuration.

A watermark may apply to:

- first page,
- body pages,
- last page,
- all pages.

It must not influence pagination.

---

# 44. Renderer architecture

The renderer consumes only the completed `PagePlan` plus document-level resources/styles.

The renderer is responsible for:

- glyph painting,
- line painting,
- fills,
- images,
- SVG,
- page creation,
- PDF metadata,
- outlines/bookmarks.

The renderer is not responsible for:

- deciding row breaks,
- deciding section breaks,
- measuring logical layout differently from the paginator,
- interpreting domain models.

---

# 45. Skia backend

`Novolis.Documents.Skia` remains the initial rendering backend.

It should continue supporting:

```csharp
DocumentPdf.Write(document, path);

byte[] bytes = DocumentPdf.ToBytes(document);

PagePlan plan = DocumentPdf.Layout(document);
```

The API may evolve naming alongside the new model, but those three capabilities remain valuable:

- file output,
- in-memory output,
- layout inspection.

---

# 46. Semantic formatting

Presentation should retain semantic information as long as practical.

Examples:

```text
Number
Date
Money
Quantity
Percentage
Identifier
```

should not become strings too early.

This provides a future path toward:

- localization,
- reusable themes,
- different invoice presentation styles,
- consistent alignment,
- alternate render targets.

Do not create dozens of semantic value types until concrete requirements justify them.

`Number` is required in v1.

Additional content types should be demand-driven.

---

# 47. Theme and style

Style is orthogonal to structure.

A document may specify:

```text
DocumentTheme
```

containing:

- typography,
- spacing,
- colors,
- table defaults,
- heading defaults,
- page chrome defaults.

Sections may override relevant values locally.

Themes should not alter document semantics.

---

# 48. Authoring API

The preferred authoring style is fluent but strongly typed.

Illustrative API:

```csharp
var document = Document.Create("Invoice")
    .Page(page => page.A4())
    .FirstPage(page => page
        .Section(section => section
            .Columns(columns => columns
                .Column(column => column
                    .Heading("INVOICE"))
                .Column(column => column
                    .Text(invoice.Number)))))
    .Section(section => section
        .Columns(columns => columns
            .Column(column => column
                .Heading("Seller")
                .Text(seller.Name)
                .Text(seller.Address))
            .Column(column => column
                .Heading("Buyer")
                .Text(buyer.Name)
                .Text(buyer.Address))))
    .Table(table => table
        .Columns(
            Column.Weight(5),
            Column.Auto(),
            Column.Auto(),
            Column.Auto())
        .Header(
            "Description",
            "Qty",
            "Price",
            "Total")
        .Rows(invoice.Lines, (row, line) => row
            .Cell(line.Description)
            .Cell(new Number(line.Quantity, NumberFormat.Quantity()))
            .Cell(new Number(line.Price, NumberFormat.Currency("NOK")))
            .Cell(new Number(line.Total, NumberFormat.Currency("NOK")))))
    .Section(section => section
        .Columns(columns => columns
            .Column()
            .Column(column => column
                .KeyValue("Net", invoice.Net)
                .KeyValue("VAT", invoice.Vat)
                .KeyValue("Total", invoice.Total))))
    .LastPage(page => page
        .Section(section => section
            .Text("Payment terms")))
    .Build();
```

Exact syntax is not normative.

The semantics are.

---

# 49. Builder invariants

Builders must produce immutable document objects.

Builders themselves may be mutable during construction.

After:

```csharp
.Build()
```

the resulting document is immutable.

This makes:

- layout deterministic,
- testing easier,
- code generation viable,
- caching possible,
- replay safe.

---

# 50. No service/repository ceremony

The library is a compositional library.

Do not introduce:

```text
DocumentService
DocumentManager
DocumentRepository
PdfCoordinator
```

without a concrete stateful responsibility.

Core functionality should remain pure wherever practical:

```text
Document → PagePlan
PagePlan → PDF
```

---

# 51. Peppol BIS Billing 3.0 acceptance target

The v1 engine is considered semantically expressive enough when a renderer-independent mapper can represent all meaningful Peppol BIS Billing 3.0 business information without using raw drawing APIs.

The existing billing model covers concepts including:

- supplier,
- customer,
- payee,
- tax representative,
- addresses,
- references,
- supporting documents,
- invoice periods,
- invoice lines,
- item identifiers,
- classifications,
- properties,
- prices,
- line allowances,
- line charges,
- document allowances,
- document charges,
- VAT categories,
- VAT breakdown,
- multiple tax totals,
- monetary totals,
- payment means,
- financial accounts,
- payment terms,
- delivery,
- notes.

The underlying normalized billing representation already treats lines, adjustments, taxes, totals, payments, references, and supporting documents as first-class concepts.

---

# 52. Peppol presentation mapping

A default invoice representation should be achievable using sections approximately like:

```text
Document
├── FirstPage
│   ├── Invoice identity
│   ├── Supplier / customer
│   └── invoice metadata
│
├── Reference Section
├── Delivery Section
├── Payment Section
├── Invoice Lines TableSection
├── Adjustment Section
├── VAT TableSection
├── Totals Section
├── Payment Terms Section
├── Supporting Documents Section
│
└── LastPage
```

Not every invoice needs every section.

Missing semantic groups simply do not emit sections.

---

# 53. Peppol invoice-lines acceptance test

The most important stress test is invoice-line pagination.

The engine must render correctly when the invoice contains:

- 1 line,
- 10 lines,
- 100 lines,
- 500 lines,
- long descriptions,
- long item names,
- multiple quantities/price formats,
- line allowances,
- line charges,
- tax information.

The line table must:

- measure rows correctly,
- wrap descriptions,
- remain aligned,
- split between rows,
- repeat its header,
- never clip rows,
- never lose rows,
- preserve ordering.

---

# 54. Peppol party-layout acceptance test

Party information must remain sensible with:

- short Norwegian addresses,
- long international addresses,
- optional trading name,
- several identifiers,
- VAT identifier,
- electronic address,
- contact name,
- phone,
- email,
- legal information.

A two-column party section should degrade sensibly when text wraps.

---

# 55. Peppol tax acceptance test

The engine must sensibly render:

- one VAT category,
- several VAT categories,
- zero-rated tax,
- exempt tax,
- reverse charge,
- multiple tax totals,
- document currency and tax currency where present.

Tax values should use semantic numeric content rather than preformatted strings.

---

# 56. Peppol totals acceptance test

The totals section must support:

- line net total,
- allowances,
- charges,
- tax exclusive amount,
- tax amount,
- tax inclusive amount,
- prepaid amount,
- rounding,
- payable amount.

These values must remain grouped sensibly.

The totals section should normally default to:

```text
KeepTogether = true
```

where it fits on an empty page.

---

# 57. Peppol supporting-document acceptance test

Supporting-document metadata may be represented without attempting to embed every attachment physically in the invoice PDF.

The document must be able to show:

- reference identifier,
- description,
- type,
- file name,
- external URL where appropriate.

Embedded attachments inside generated PDF files are outside initial v1 scope.

---

# 58. Peppol references acceptance test

The layout must sensibly support arbitrary valid collections of references, including:

- purchase order,
- sales order,
- preceding invoice,
- contract,
- project,
- despatch advice,
- receipt advice,
- tender/lot,
- invoiced object.

These should not require new PDF primitives.

---

# 59. Peppol completeness criterion

The decisive architecture test is:

> Given a valid normalized Peppol BIS Billing 3.0 `BillingDocument`, every business concept that a sensible human-readable invoice would reasonably expose can be mapped into `Document`, `ISection`, `IContainer`, and `IContent` without accessing Skia or PDF drawing primitives.

A mapping may choose not to visibly print purely transport-oriented metadata.

The document engine must nevertheless not be structurally incapable of expressing relevant billing information.

---

# 60. Acceptance fixtures

At minimum, tests should include:

### Minimal invoice

One line and only required/ordinary fields.

### Large invoice

At least 300 invoice lines.

### Long text invoice

Long:

- supplier name,
- buyer name,
- addresses,
- notes,
- descriptions,
- references.

### Tax-heavy invoice

Several VAT categories.

### Adjustment-heavy invoice

Multiple document and line allowances/charges.

### Reference-heavy invoice

All supported business reference types.

### Payment-heavy invoice

Several payment instructions and terms.

### International invoice

Long addresses, differing currencies, Unicode names.

### Credit note

Same overall presentation model with appropriate semantic labeling.

### Pathological-but-valid invoice

Valid data designed to maximize wrapping and pagination pressure.

---

# 61. Visual acceptance tests

PDF output should be tested with controlled fixtures.

Tests should assert structural behavior first:

- page count,
- fragment count,
- repeated table headers,
- row distribution,
- no dropped content,
- correct page kinds.

Renderer tests may additionally compare:

- dimensions,
- generated text extraction,
- deterministic metadata where possible,
- selected visual snapshots.

Avoid brittle byte-for-byte PDF assertions.

---

# 62. Layout tests

Most tests should stop before PDF creation.

Example:

```csharp
var plan = paginator.Layout(document);

plan.Pages.Should().HaveCount(3);

plan.Pages[1]
    .Fragments
    .OfType<TableFragment>()
    .Single()
    .HasRepeatedHeader
    .Should()
    .BeTrue();
```

The layout engine should be testable without Skia by using deterministic fake text metrics.

Renderer integration tests verify real-font behavior separately.

---

# 63. Real rendering tests

Skia integration tests must verify:

- actual font measurement,
- line wrapping,
- tables,
- SVG,
- raster images,
- PDF output,
- Unicode using configured fonts.

Tests should generate real PDF bytes rather than mocking Skia.

---

# 64. Layout failures

Layout failures should be diagnostic and typed.

Examples:

```text
SectionTooLarge
TableRowTooLarge
NoUsablePageArea
InvalidColumnConfiguration
UnsatisfiableKeepTogether
UnsupportedContent
```

A diagnostic should include useful context such as:

- section type,
- row index,
- available width,
- available height,
- measured height.

Do not hide these behind generic `InvalidOperationException` where a dedicated diagnostic materially improves debugging.

---

# 65. Performance

Target workloads include invoices with hundreds or low thousands of rows.

Pagination should be approximately linear with document content.

Avoid:

- repeatedly measuring unchanged text,
- reparsing semantic content,
- renderer allocation per glyph,
- retaining large temporary object graphs unnecessarily.

Caching repeated measurements is permitted where deterministic.

---

# 66. Memory

`ToBytes` necessarily retains the complete PDF output.

A future streaming API may be added.

The layout model itself should not require retaining renderer-native page bitmaps.

PDF should remain vector/text where the renderer supports it.

---

# 67. Thread safety

Built `Document` instances are immutable and safe to share.

Renderer and layout contexts may use per-operation state.

No global mutable theme or font registry should be required.

---

# 68. Async

Core layout is CPU-bound and should remain synchronous.

Do not add `Task` merely for API fashion.

Async belongs at actual I/O boundaries where necessary.

For example, a future stream-writing API may support:

```csharp
ValueTask WriteAsync(...)
```

if it performs asynchronous I/O.

---

# 69. Dependency injection

Core composition should not require DI.

Consumers may register:

- renderer,
- theme provider,
- domain mappers,

through DI where useful.

Simple use must remain:

```csharp
var document = ...;
var pdf = DocumentPdf.ToBytes(document);
```

---

# 70. Extensibility

The library should support adding a new section implementation without modifying every existing section.

The paginator should dispatch through registered or intrinsic section layout behavior rather than one enormous renderer switch where practical.

However, avoid abstract factories and plugin systems until multiple real implementations require them.

---

# 71. Domain mapping

Domain-specific mapping belongs outside core documents.

For example:

```text
BillingDocument
    ↓
BillingDocumentMapper
    ↓
Document
```

The generated `Document` has no knowledge that its source was UBL or Peppol.

This aligns with the existing normalized billing philosophy: domain information is promoted into a strongly typed representation rather than leaking source XML structure into ordinary application code.

---

# 72. Arbitrary UBL future direction

Arbitrary UBL support must not cause the document engine to become UBL-aware.

The future architecture should remain:

```text
UBL
 ↓
domain/source interpretation
 ↓
Document
 ↓
layout
 ↓
PDF
```

If a future arbitrary UBL renderer needs additional document primitives, those primitives should be added only when they are independently sensible document concepts.

---

# 73. Migration from current `novolis-documents`

The existing implementation should evolve rather than be discarded.

Current:

```text
PagedDocument
    ↓
Blocks[]
    ↓
DocumentPaginator
    ↓
PagePlan
    ↓
DocumentPdf
```

Target:

```text
Document
    ↓
ISection[]
    ↓
section layout
    ↓
PagePlan
    ↓
DocumentPdf
```

Existing concepts may migrate approximately as follows:

```text
ParagraphBlock
    → Text

HeadingBlock
    → Heading

ImageBlock
    → Image

ColumnsBlock
    → ColumnsSection / Column

TableBlock
    → TableSection

PageBreakBlock
    → PageBreakSection

SceneBreakBlock
    → Rule / dedicated semantic content if still justified
```

Do not mechanically rename everything.

Use the migration to simplify responsibilities.

---

# 74. Existing functionality to preserve

The rewrite must preserve or replace current useful behavior:

- A4/trade-page setup,
- margins,
- first page,
- table of contents where applicable,
- body pages,
- last page,
- headers,
- footers,
- watermarks,
- text,
- headings,
- tables,
- repeating table headers,
- columns,
- images,
- SVG,
- code/text boxes if retained,
- PDF bookmarks/outlines,
- embedded/default fonts,
- custom fonts,
- file output,
- byte output,
- inspectable layout plan.

---

# 75. Suggested implementation stages

## Phase 1: Model

Introduce:

```text
Document
ISection
IContent
IContainer
Section
Text
Number
Heading
Column
ColumnsSection
```

No PDF changes required initially.

---

## Phase 2: Layout contract

Introduce:

```text
SectionLayoutResult
SectionFragment
PlacedFragment
```

Adapt current pagination around section flow.

---

## Phase 3: Basic rendering

Render:

- text,
- number,
- heading,
- normal section,
- columns.

Verify first/body/last page semantics.

---

## Phase 4: TableSection

Implement:

- width resolution,
- cells,
- row measurement,
- repeated headers,
- row-based page splitting,
- oversized-row failure.

This is the most important implementation milestone.

---

## Phase 5: Billing feature tests

Build a small:

```text
BillingDocument → Document
```

mapper.

It exists initially as a test/dogfood consumer rather than necessarily a published package.

Use the Peppol-oriented normalized billing model as the test corpus.

---

## Phase 6: Polish

Add:

- theme refinements,
- page outlines,
- richer images,
- useful diagnostics,
- performance improvements.

---

# 76. Definition of v1 complete

Version 1 is complete when all of the following are true:

1. A document consists of a vertical sequence of sections.

2. Sections may contain semantic content.

3. Columns work inside sections.

4. Tables are first-class sections.

5. Tables split automatically between rows.

6. Table headers repeat automatically on continuation pages.

7. First pages may be unique.

8. Last pages may be unique.

9. Headers and footers work independently of section flow.

10. Text wrapping and pagination use the same real font measurements as rendering.

11. `Document → PagePlan` is renderer-independent.

12. `PagePlan → PDF` works through Skia.

13. PDF output is available as file and bytes.

14. Normal layout does not require renderer-specific APIs.

15. A complete Peppol BIS Billing 3.0 invoice can be mapped into the model sensibly.

16. Large invoices paginate deterministically.

17. No valid invoice line disappears or clips because of pagination.

18. Real PDF rendering tests exist.

19. Layout behavior is predominantly tested without the renderer.

20. No dependency on QuestPDF, Markdig, HTML/CSS engines, or equivalent external document-layout frameworks is required.

---

# 77. Core architectural invariant

The most important invariant of the entire library is:

> **The document is a vertical sequence of sections. A section determines how its own content is arranged and how it may fragment across pages.**

Everything else follows from that.

It prevents the public API from collapsing into generic box layout.

It gives tables a natural place to own pagination behavior.

It keeps columns local.

It keeps domain mapping readable.

And it makes the engine powerful enough for serious documents without accidentally rebuilding a browser.
