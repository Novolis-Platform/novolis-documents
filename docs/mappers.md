# Mapping into PagedDocument

The fluent DSL is for hand-authored samples and product hosts. Pipelines usually build the immutable model with object initializers (or their own builders) and call `DocumentPdf`.

## Pattern

```csharp
static PagedDocument Map(MySource source) => new()
{
    Meta = new DocumentMeta
    {
        Title = source.Title,
        Author = source.Issuer,
        Identifier = source.Id,
        Language = "en",
        Date = source.IssueDate,
    },
    Setup = new PageSetup
    {
        Trim = TrimPresets.A4,
        Margin = TrimPresets.ReportMargin,
        HeaderBand = Length.FromPoints(14),
        FooterBand = Length.FromPoints(14),
    },
    Typography = new Typography
    {
        BodyFontSizePt = 9f,
        TableFontSizePt = 8f,
    },
    Footer = new Footer
    {
        Template = "{page} / {pages}",
        IncludeBody = true,
        IncludeLastPage = true,
    },
    IncludeCover = false,
    IncludeToc = false,
    Body = BuildBlocks(source),
};

static IReadOnlyList<IBlock> BuildBlocks(MySource source) =>
[
    new ImageBlock { Path = source.LogoPath, Width = Length.FromPoints(120), Height = Length.FromPoints(40) },
    new ColumnsBlock
    {
        Gap = Length.FromPoints(16),
        Fractions = [0.5f, 0.5f],
        Columns =
        [
            [new HeadingBlock { Level = 3, Text = "From" }, new ParagraphBlock { Text = source.Seller }],
            [new HeadingBlock { Level = 3, Text = "To" }, new ParagraphBlock { Text = source.Buyer }],
        ],
    },
    new TableBlock
    {
        Headers = ["#", "Description", "Amount"],
        Rows = source.Lines.Select(l => (IReadOnlyList<string>)[l.No, l.Desc, l.Amount]).ToArray(),
        ColumnWidths = [0.1f, 0.65f, 0.25f],
        ColumnAlignments = [CellAlign.Left, CellAlign.Left, CellAlign.Right],
        Rules = TableRuleStyle.Horizontal,
        HeaderBackground = true,
    },
];
```

Keep **domain vocabulary** in the mapper (`Seller`, `UBL`, `Invoice`) — not in `Novolis.Documents` public types.

## In-repo experiment: UBL invoice

Tests only (not a published package):

| File | Role |
| --- | --- |
| `d:\novolis\novolis-documents\tests\Novolis.Documents.Unit\Ubl\UblInvoiceDocumentMapper.cs` | Lean UBL invoice → `PagedDocument` |
| `d:\novolis\novolis-documents\tests\Novolis.Documents.Unit\Ubl\UblInvoicePdfTests.cs` | Writes PDF under artifacts |

Layout language is Norwegian-style sections; copy can stay English. Uses A4, logo SVG, columns for parties/payment, rich line table.

Artifact path used by the test:

`C:\Users\frank\.novolis\artifacts\ubl-invoice\TOSL108-invoice.pdf`

## Markup bridge

Markdown → Documents lives outside this repo (dogfood: `HelloMarkdownPdf`). Pattern: parse markup → map AST nodes to `IBlock` → `DocumentPdf`.

## When to use Fluent vs mapper

| Situation | Prefer |
| --- | --- |
| Samples, labs, small reports | `Document.Create` |
| XSD/UBL/codegen/import pipelines | Object initializers / custom mapper |
| Partial reuse of DSL inside a mapper | Nested builders (`TableBuilder`, `ColumnsBuilder`) without the full spine |
