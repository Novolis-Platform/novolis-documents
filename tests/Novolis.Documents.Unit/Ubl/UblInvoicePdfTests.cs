using Novolis.Documents;
using Novolis.Documents.Skia;
using Novolis.Xsd.Ubl;
using Novolis.Xsd.Ubl.Invoice;
using Novolis.Xsd.Ubl.Lean;
using TUnit.Core;

namespace Novolis.Documents.Unit.Ubl;

public sealed class UblInvoicePdfTests
{
    [Test]
    public async Task Oasis_sample_maps_to_document_with_lines_and_payable()
    {
        var lean = LoadOasisSample();
        var doc = UblInvoiceDocumentMapper.FromLean(lean);

        await Assert.That(doc.Meta.Title).Contains("TOSL108");
        await Assert.That(doc.First).IsNotNull();
        await Assert.That(doc.Body.OfType<TableBlock>().Count()).IsGreaterThanOrEqualTo(2);
        await Assert.That(doc.Body.OfType<HeadingBlock>().Any(h => h.Text == "Invoice")).IsTrue();

        var lineTable = doc.Body.OfType<TableBlock>().First(t => t.Headers.Contains("Description"));
        await Assert.That(lineTable.Rows.Count).IsEqualTo(lean.InvoiceLine.Count);
        await Assert.That(lineTable.Rows.Count).IsGreaterThan(0);
        await Assert.That(lean.LegalMonetaryTotal.PayableAmount.Value).IsEqualTo(729m);
    }

    [Test]
    public async Task Oasis_sample_renders_invoice_pdf_artifact()
    {
        var lean = LoadOasisSample();
        var doc = UblInvoiceDocumentMapper.FromLean(lean);
        var bytes = DocumentPdf.ToBytes(doc);

        await Assert.That(bytes.Length).IsGreaterThan(2000);
        await Assert.That(bytes.Length).IsLessThan(120_000);
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
        Console.WriteLine($"Bytes: {bytes.Length}");
    }

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
