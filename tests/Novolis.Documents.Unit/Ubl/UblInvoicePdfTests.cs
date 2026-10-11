using Novolis.Documents;
using Novolis.Documents.Layout;
using Novolis.Documents.Skia;
using Novolis.Xsd.Ubl;
using Novolis.Xsd.Ubl.Invoice;
using Novolis.Xsd.Ubl.Lean;
using TUnit.Core;

namespace Novolis.Documents.Unit.Ubl;

public sealed class UblInvoicePdfTests
{
    [Test]
    public async Task Oasis_sample_maps_to_invoice_layout_structure()
    {
        var lean = LoadOasisSample();
        var doc = UblInvoiceDocumentMapper.FromLean(lean);

        await Assert.That(doc.Meta.Title).Contains("TOSL108");
        await Assert.That(doc.FirstPage).IsNull();
        await Assert.That(doc.LastPage).IsNull();
        var columns = doc.Sections.OfType<ColumnsSection>().ToArray();
        await Assert.That(columns.Any(c => c.Columns.Any(column => column.Contents.OfType<Image>().Any())))
            .IsTrue();
        await Assert.That(columns.Length).IsGreaterThanOrEqualTo(3);
        await Assert.That(columns.Any(c =>
                c.Columns.Any(column => column.Contents.OfType<Heading>().Any(h => h.Value == "Payment"))))
            .IsTrue();

        var lineTable = doc.Sections
            .OfType<TableSection>()
            .Single(table => table.Columns.Count >= 8);
        await Assert.That(lineTable.Columns.Count).IsGreaterThanOrEqualTo(8);
        await Assert.That(lineTable.Options.RuleStyle).IsEqualTo(TableRuleStyle.Horizontal);
        await Assert.That(lineTable.Rows.Count).IsEqualTo(lean.InvoiceLine.Count);
        await Assert.That(lineTable.Rows.Count).IsGreaterThan(0);
        await Assert.That(lean.LegalMonetaryTotal.PayableAmount.Value).IsEqualTo(729m);
    }

    [Test]
    public async Task Oasis_sample_renders_invoice_pdf_artifact()
    {
        var lean = LoadOasisSample();
        var doc = UblInvoiceDocumentMapper.FromLean(lean);
        var plan = DocumentPdf.Layout(doc);
        await Assert.That(plan.Pages.Count).IsGreaterThanOrEqualTo(1);

        var bytes = DocumentPdf.ToBytes(doc);

        await Assert.That(bytes.Length).IsGreaterThan(2000);
        await Assert.That(bytes.Length).IsLessThan(200_000);
        await Assert.That(bytes[0]).IsEqualTo((byte)'%');
        await Assert.That(bytes[1]).IsEqualTo((byte)'P');

        var outDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".novolis",
            "artifacts",
            "ubl-invoice");
        Directory.CreateDirectory(outDir);
        var path = Path.Combine(outDir, "TOSL108-invoice.pdf");
        await File.WriteAllBytesAsync(path, bytes);

        await Assert.That(File.Exists(path)).IsTrue();
        Console.WriteLine(path);
        Console.WriteLine($"Bytes: {bytes.Length}; pages: {plan.Pages.Count}");
    }

    [Test]
    public async Task Peppol_shaped_large_invoice_preserves_all_line_rows()
    {
        var source = UblInvoiceDocumentMapper.FromLean(LoadOasisSample());
        var sourceTable = source.Sections
            .OfType<TableSection>()
            .Single(table => table.Columns.Count >= 8);
        var rows = Enumerable.Range(0, 300)
            .Select(index => sourceTable.Rows[index % sourceTable.Rows.Count])
            .ToArray();
        var largeTable = new TableSection(
            sourceTable.Columns,
            rows,
            sourceTable.Options,
            sourceTable.Layout);
        var sections = source.Sections
            .Select(section => ReferenceEquals(section, sourceTable)
                ? (ISection)largeTable
                : section)
            .ToArray();
        var large = CopyWithSections(source, sections);

        var plan = DocumentPdf.Layout(large);
        var fragments = plan.Pages
            .SelectMany(static page => page.Fragments)
            .Select(static placed => placed.Fragment)
            .OfType<TableFragment>()
            .Where(static fragment => fragment.Table.Columns.Count >= 8)
            .ToArray();

        await Assert.That(fragments.Length).IsGreaterThan(1);
        await Assert.That(fragments.SelectMany(static fragment => fragment.Rows).Count)
            .IsEqualTo(300);
        await Assert.That(fragments.Skip(1).All(static fragment => fragment.HasRepeatedHeader))
            .IsTrue();

        var bytes = DocumentPdf.ToBytes(large);
        await Assert.That(bytes.Length).IsGreaterThan(2000);
        await Assert.That(bytes[0]).IsEqualTo((byte)'%');
        await Assert.That(bytes[1]).IsEqualTo((byte)'P');
    }

    static Document CopyWithSections(Document source, IReadOnlyList<ISection> sections) => new()
    {
        Meta = source.Meta,
        Setup = source.Setup,
        Typography = source.Typography,
        Sections = sections,
        FirstPage = source.FirstPage,
        LastPage = source.LastPage,
        Header = source.Header,
        Footer = source.Footer,
        Watermark = source.Watermark,
        Theme = source.Theme,
        IncludeToc = source.IncludeToc,
    };

    static InvoiceBase LoadOasisSample()
    {
        var path = FindUblSample("UBL-Invoice-2.1-Example.xml");
        var xml = File.ReadAllText(path);
        var wire = UblDocument.Parse<InvoiceType>(xml);
        return InvoiceBaseMapper.ToBase(wire);
    }

    static string FindUblSample(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            foreach (var candidate in new[]
                     {
                         Path.Combine(dir.FullName, "TestData", fileName),
                         Path.Combine(dir.FullName, "tests", "Novolis.Documents.Unit", "TestData", fileName),
                         Path.Combine(dir.FullName, "novolis-xsd", "tests", "Novolis.Xsd.Unit", "TestData", fileName),
                     })
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"UBL sample not found: {fileName}");
    }
}
