using System.Globalization;
using System.Text;
using Novolis.Documents;
using Novolis.Math.Measure;
using Novolis.Xsd.Ubl.Lean;

namespace Novolis.Documents.Unit.Ubl;

/// <summary>
/// Experimental mapper: UBL Lean invoice → <see cref="PagedDocument"/>.
/// Not a product API — lives in tests while we learn the layout vocabulary.
/// </summary>
internal static class UblInvoiceDocumentMapper
{
    public static PagedDocument FromLean(InvoiceBase invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        var currency = invoice.DocumentCurrencyCode?.Value
            ?? invoice.LegalMonetaryTotal.PayableAmount.currencyID
            ?? "EUR";
        var invoiceId = invoice.ID.Value;
        var supplier = FormatParty(invoice.AccountingSupplierParty.Party);
        var customer = FormatParty(invoice.AccountingCustomerParty.Party);
        var supplierName = PartyName(invoice.AccountingSupplierParty.Party) ?? "Supplier";
        var customerName = PartyName(invoice.AccountingCustomerParty.Party) ?? "Customer";

        var blocks = new List<IBlock>
        {
            new HeadingBlock { Level = 1, Text = "Invoice" },
            new ParagraphBlock
            {
                Text = $"No. {invoiceId}  ·  Issued {invoice.IssueDate:yyyy-MM-dd}"
                    + (invoice.DueDate is { } due ? $"  ·  Due {due:yyyy-MM-dd}" : string.Empty)
                    + $"  ·  {currency}",
            },
            new HeadingBlock { Level = 2, Text = "Supplier" },
            new ParagraphBlock { Text = supplier },
            new HeadingBlock { Level = 2, Text = "Bill to" },
            new ParagraphBlock { Text = customer },
        };

        if (invoice.OrderReference?.ID is { Value: { Length: > 0 } orderId })
            blocks.Add(new ParagraphBlock { Text = $"Order reference: {orderId}" });

        if (invoice.InvoicePeriod is { Count: > 0 } periods)
        {
            var p = periods[0];
            var start = p.StartDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "…";
            var end = p.EndDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "…";
            blocks.Add(new ParagraphBlock { Text = $"Invoice period: {start} – {end}" });
        }

        foreach (var note in invoice.Note)
        {
            if (!string.IsNullOrWhiteSpace(note.Value))
                blocks.Add(new ParagraphBlock { Text = $"Note: {note.Value}" });
        }

        blocks.Add(new HeadingBlock { Level = 2, Text = "Line items" });
        blocks.Add(BuildLineTable(invoice, currency));

        blocks.Add(new HeadingBlock { Level = 2, Text = "Totals" });
        blocks.Add(BuildTotalsTable(invoice, currency));

        if (invoice.TaxTotal.Count > 0)
        {
            blocks.Add(new HeadingBlock { Level = 3, Text = "Tax" });
            blocks.Add(BuildTaxTable(invoice, currency));
        }

        var paymentLines = BuildPaymentLines(invoice);
        LastPage? last = paymentLines.Count > 0
            ? new LastPage
            {
                Title = "Payment",
                Lines = paymentLines,
            }
            : null;

        return new PagedDocument
        {
            Meta = new DocumentMeta
            {
                Title = $"Invoice {invoiceId}",
                Subtitle = $"{supplierName} → {customerName}",
                Author = supplierName,
                Rights = $"UBL 2.1 · {currency}",
            },
            Setup = new PageSetup
            {
                Trim = TrimPresets.USLetter,
                Margin = new Thickness(
                    LengthUnits.FromInches(0.7f),
                    LengthUnits.FromInches(0.55f),
                    LengthUnits.FromInches(0.55f),
                    LengthUnits.FromInches(0.6f)),
            },
            Typography = new Typography
            {
                BodyFontSizePt = 10f,
                H1SizePt = 20f,
                H2SizePt = 12f,
                H3SizePt = 11f,
                LineHeight = 1.25f,
                ParagraphSpacingPt = 5f,
                AfterLevel1SpacingPt = 8f,
                AfterHeadingSpacingPt = 4f,
                TableFontSizePt = 9f,
                TableCellPaddingPt = 3.5f,
                TableRuleStrokePt = 0.4f,
            },
            IncludeCover = false,
            IncludeToc = false,
            First = new FirstPage
            {
                Title = "INVOICE",
                Subtitle = invoiceId,
                Author = supplierName,
                Lines =
                [
                    $"Issue date {invoice.IssueDate:dd MMM yyyy}",
                    invoice.DueDate is { } d ? $"Due {d:dd MMM yyyy}" : "Due on receipt",
                    $"Currency {currency}",
                ],
            },
            Header = new RunningChrome { Template = $"Invoice {invoiceId} — {{title}}", FontSizePt = 8f },
            Footer = new RunningChrome { Template = "{page}", FontSizePt = 8f },
            SuppressHeaderOnLevel1Open = true,
            Last = last,
            Body = blocks,
        };
    }

    static TableBlock BuildLineTable(InvoiceBase invoice, string currency)
    {
        var rows = new List<IReadOnlyList<string>>();
        foreach (var line in invoice.InvoiceLine)
        {
            var name = line.Item.Name?.Value
                ?? line.Item.Description.FirstOrDefault()?.Value
                ?? "(item)";
            var qty = line.InvoicedQuantity is { } q
                ? $"{q.Value.ToString("0.##", CultureInfo.InvariantCulture)}{(string.IsNullOrWhiteSpace(q.unitCode) ? "" : " " + q.unitCode)}"
                : "—";
            var unit = line.Price?.PriceAmount is { } pa
                ? Money(pa.Value, pa.currencyID ?? currency)
                : "—";
            var ext = Money(line.LineExtensionAmount.Value, line.LineExtensionAmount.currencyID ?? currency);
            rows.Add([line.ID.Value, name, qty, unit, ext]);
        }

        return new TableBlock
        {
            Headers = ["#", "Description", "Qty", "Unit", "Amount"],
            Rows = rows,
            ShowHeader = true,
            DrawRules = true,
            RepeatHeaderOnPageBreak = true,
        };
    }

    static TableBlock BuildTotalsTable(InvoiceBase invoice, string currency)
    {
        var m = invoice.LegalMonetaryTotal;
        var rows = new List<IReadOnlyList<string>>();
        if (m.LineExtensionAmount is { } lea)
            rows.Add(["Line extension", Money(lea.Value, lea.currencyID ?? currency)]);
        if (m.TaxExclusiveAmount is { } tea)
            rows.Add(["Tax exclusive", Money(tea.Value, tea.currencyID ?? currency)]);
        if (m.TaxInclusiveAmount is { } tia)
            rows.Add(["Tax inclusive", Money(tia.Value, tia.currencyID ?? currency)]);
        if (m.AllowanceTotalAmount is { } ata)
            rows.Add(["Allowances", Money(ata.Value, ata.currencyID ?? currency)]);
        if (m.ChargeTotalAmount is { } cta)
            rows.Add(["Charges", Money(cta.Value, cta.currencyID ?? currency)]);
        if (m.PrepaidAmount is { } ppa)
            rows.Add(["Prepaid", Money(ppa.Value, ppa.currencyID ?? currency)]);
        rows.Add(["Payable", Money(m.PayableAmount.Value, m.PayableAmount.currencyID ?? currency)]);

        return new TableBlock
        {
            Headers = ["", ""],
            Rows = rows,
            ShowHeader = false,
            DrawRules = true,
        };
    }

    static TableBlock BuildTaxTable(InvoiceBase invoice, string currency)
    {
        var rows = new List<IReadOnlyList<string>>();
        foreach (var tax in invoice.TaxTotal)
        {
            rows.Add(["Tax total", Money(tax.TaxAmount.Value, tax.TaxAmount.currencyID ?? currency)]);
            foreach (var sub in tax.TaxSubtotal)
            {
                var cat = sub.TaxCategory?.ID?.Value ?? "VAT";
                var pct = sub.TaxCategory?.Percent?.Value;
                var label = pct is { } p
                    ? $"{cat} {p.ToString("0.##", CultureInfo.InvariantCulture)}%"
                    : cat;
                rows.Add([label, Money(sub.TaxAmount.Value, sub.TaxAmount.currencyID ?? currency)]);
            }
        }

        return new TableBlock
        {
            Headers = ["Category", "Amount"],
            Rows = rows,
            ShowHeader = true,
            DrawRules = true,
        };
    }

    static List<string> BuildPaymentLines(InvoiceBase invoice)
    {
        var lines = new List<string>();
        foreach (var means in invoice.PaymentMeans)
        {
            if (means.PaymentMeansCode?.Value is { Length: > 0 } code)
                lines.Add($"Payment means code: {code}");
            if (means.PaymentDueDate is { } due)
                lines.Add($"Payment due: {due:yyyy-MM-dd}");
            var account = means.PayeeFinancialAccount;
            if (account?.ID?.Value is { Length: > 0 } iban)
                lines.Add($"Account: {iban}");
            if (account?.Name?.Value is { Length: > 0 } accName)
                lines.Add($"Account name: {accName}");
        }

        foreach (var terms in invoice.PaymentTerms)
        {
            foreach (var note in terms.Note)
            {
                if (!string.IsNullOrWhiteSpace(note.Value))
                    lines.Add(note.Value);
            }
        }

        return lines;
    }

    static string? PartyName(PartyBase? party) =>
        party?.PartyName.FirstOrDefault()?.Name?.Value
        ?? party?.PartyLegalEntity.FirstOrDefault()?.RegistrationName?.Value;

    static string FormatParty(PartyBase? party)
    {
        if (party is null)
            return "—";

        var sb = new StringBuilder();
        var name = PartyName(party);
        if (!string.IsNullOrWhiteSpace(name))
            sb.AppendLine(name);

        if (party.PostalAddress is { } addr)
        {
            Append(sb, JoinNonEmpty(" ", addr.StreetName?.Value, addr.BuildingNumber?.Value));
            Append(sb, addr.AdditionalStreetName?.Value);
            Append(sb, addr.Department?.Value);
            Append(sb, JoinNonEmpty(" ", addr.PostalZone?.Value, addr.CityName?.Value));
            Append(sb, addr.CountrySubentity?.Value ?? addr.CountrySubentityCode?.Value);
            Append(sb, addr.Country?.IdentificationCode?.Value);
        }

        foreach (var tax in party.PartyTaxScheme)
        {
            if (tax.CompanyID?.Value is { Length: > 0 } vat)
                Append(sb, $"Tax ID: {vat}");
        }

        if (party.Contact is { } contact)
        {
            Append(sb, contact.ElectronicMail?.Value);
            Append(sb, contact.Telephone?.Value is { Length: > 0 } tel ? $"Tel {tel}" : null);
        }

        return sb.Length == 0 ? "—" : sb.ToString().TrimEnd();
    }

    static void Append(StringBuilder sb, string? line)
    {
        if (!string.IsNullOrWhiteSpace(line))
            sb.AppendLine(line.Trim());
    }

    static string? JoinNonEmpty(string sep, params string?[] parts)
    {
        var list = parts.Where(static p => !string.IsNullOrWhiteSpace(p)).Select(static p => p!.Trim()).ToArray();
        return list.Length == 0 ? null : string.Join(sep, list);
    }

    static string Money(decimal amount, string currency) =>
        string.Create(CultureInfo.InvariantCulture, $"{currency} {amount:0.00}");
}
