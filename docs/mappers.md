# Mapping into Document

The fluent DSL is for hand-authored samples and product hosts. Pipelines usually build the immutable semantic model with object initializers (or their own builders) and call `DocumentPdf`.

## Pattern

```csharp
static Document Map(MySource source) => new()
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
        IncludeBody = true, // First / Toc / Last also default off
    },
    IncludeCover = false,
    IncludeToc = false,
    Sections = BuildSections(source),
};

static IReadOnlyList<ISection> BuildSections(MySource source) =>
[
    new Section([new Image(source.LogoPath, 120f, 40f)]),
    new ColumnsSection(
        [
            new Column(
            [
                new Heading("From", 3),
                new Text(source.Seller),
            ], ColumnWidth.WeightBy(1)),
            new Column(
            [
                new Heading("To", 3),
                new Text(source.Buyer),
            ], ColumnWidth.WeightBy(1)),
        ],
        gapPt: 16f),
    new TableSection(
        [
            new TableColumn(ColumnWidth.WeightBy(0.1f)),
            new TableColumn(ColumnWidth.WeightBy(0.65f)),
            new TableColumn(ColumnWidth.WeightBy(0.25f), HorizontalAlignment.Right),
        ],
        source.Lines.Select(line => new TableRow(
        [
            new TableCell(line.No),
            new TableCell(line.Desc),
            new TableCell(new Number(decimal.Parse(line.Amount, CultureInfo.InvariantCulture),
                NumberFormat.Currency(source.Currency))),
        ])),
        new TableOptions
        {
            Header = new TableRow("#", "Description", "Amount"),
            HeaderBackground = true,
            RuleStyle = TableRuleStyle.Horizontal,
        }),
];
```

Keep **domain vocabulary** in the mapper (`Seller`, `UBL`, `Invoice`) — not in `Novolis.Documents` public types.

## In-repo experiment: UBL invoice

Tests only (not a published package):

| File | Role |
| --- | --- |
| `d:\novolis\novolis-documents\tests\Novolis.Documents.Unit\Ubl\UblInvoiceDocumentMapper.cs` | Lean UBL invoice → `Document` |
| `d:\novolis\novolis-documents\tests\Novolis.Documents.Unit\Ubl\UblInvoicePdfTests.cs` | Writes PDF under artifacts |

Layout language is Norwegian-style sections; copy can stay English. Uses A4, logo SVG, columns for parties/payment, rich line table.

Artifact path used by the test:

`C:\Users\frank\.novolis\artifacts\ubl-invoice\TOSL108-invoice.pdf`

## Markup bridge

Markdown → Documents lives outside this repo (dogfood: `HelloMarkdownPdf`). Pattern: parse markup → map AST nodes to `ISection` / `IContent` → `DocumentPdf`.

## When to use Fluent vs mapper

| Situation | Prefer |
| --- | --- |
| Samples, labs, small reports | `Document.Create` |
| XSD/UBL/codegen/import pipelines | Object initializers / custom mapper |
| Partial reuse of DSL inside a mapper | Nested builders (`TableBuilder`, `ColumnsBuilder`) without the full spine |
