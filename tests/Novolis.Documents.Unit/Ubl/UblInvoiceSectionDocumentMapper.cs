using System.Globalization;
using System.Text;
using Novolis.Documents;
using Novolis.Math.Measure;
using Novolis.Xsd.Ubl.Lean;

namespace Novolis.Documents.Unit.Ubl;

/// <summary>Dogfood mapper from the normalized UBL Lean billing model to sections.</summary>
internal static class UblInvoiceDocumentMapper
{
    static readonly CultureInfo Nb = CultureInfo.GetCultureInfo("nb-NO");

    public static Document FromLean(InvoiceBase invoice, string? logoPath = null)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        var currency = invoice.DocumentCurrencyCode?.Value
            ?? invoice.LegalMonetaryTotal.PayableAmount.currencyID
            ?? "EUR";
        var invoiceId = invoice.ID.Value;
        var supplierName = PartyName(invoice.AccountingSupplierParty.Party) ?? "Supplier";
        var customerName = PartyName(invoice.AccountingCustomerParty.Party) ?? "Customer";
        var sections = new List<ISection>();
        var logo = ResolveLogoPath(logoPath);

        var titleContents = new List<IContent>
        {
            new Heading("INVOICE", 2),
            new Text(supplierName),
        };
        var metadataContents = new List<IContent>
        {
            new KeyValue("No.", invoiceId),
            new KeyValue("Issued", FormatDate(invoice.IssueDate)),
            new KeyValue("Due", invoice.DueDate is { } due ? FormatDate(due) : "On receipt"),
            new KeyValue("Currency", currency),
        };
        var letterhead = new List<Column>();
        if (!string.IsNullOrWhiteSpace(logo) && File.Exists(logo))
            letterhead.Add(new Column([new Image(logo, 40f, 40f)], new ColumnWidth.Weight(1f)));
        letterhead.Add(new Column(titleContents, new ColumnWidth.Weight(5f)));
        letterhead.Add(new Column(metadataContents, new ColumnWidth.Weight(4f)));
        sections.Add(new ColumnsSection(letterhead, 12f));

        sections.Add(new ColumnsSection(
            [
                new Column(
                    [
                        new Heading("Supplier", 3),
                        new Text(FormatParty(invoice.AccountingSupplierParty.Party)),
                    ],
                    new ColumnWidth.Weight(1f)),
                new Column(
                    [
                        new Heading("Bill to", 3),
                        new Text(FormatParty(invoice.AccountingCustomerParty.Party)),
                    ],
                    new ColumnWidth.Weight(1f)),
            ],
            20f));

        var references = new List<IContent>();
        if (invoice.OrderReference?.ID is { Value: { Length: > 0 } orderId })
            references.Add(new KeyValue("Order", orderId));
        if (invoice.InvoicePeriod is { Count: > 0 } periods)
        {
            var period = periods[0];
            var start = period.StartDate is { } startDate ? FormatDate(startDate) : "…";
            var end = period.EndDate is { } endDate ? FormatDate(endDate) : "…";
            references.Add(new KeyValue("Period", $"{start} – {end}"));
        }

        if (references.Count > 0)
            sections.Add(new Section(references));

        foreach (var note in invoice.Note ?? [])
        {
            if (!string.IsNullOrWhiteSpace(note.Value))
                sections.Add(new Section([new Text(note.Value)]));
        }

        sections.Add(BuildLineTable(invoice, currency));

        var paymentContents = new List<IContent>
        {
            new Heading("Payment", 3),
            new Text(BuildPaymentBody(invoice, currency)),
        };
        sections.Add(new ColumnsSection(
            [
                new Column(paymentContents, new ColumnWidth.Weight(58f)),
                new Column(BuildSummaryContents(invoice, currency), new ColumnWidth.Weight(42f)),
            ],
            22f));

        return new Document
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
                Trim = TrimPresets.A4,
                Margin = new Thickness(
                    LengthUnits.FromMillimeters(14f),
                    LengthUnits.FromMillimeters(12f),
                    LengthUnits.FromMillimeters(14f),
                    LengthUnits.FromMillimeters(12f)),
                HeaderBand = LengthUnits.FromPoints(0f),
                FooterBand = LengthUnits.FromPoints(12f),
            },
            Typography = new Typography
            {
                BodyFontSizePt = 8.5f,
                H1SizePt = 14f,
                H2SizePt = 13f,
                H3SizePt = 8.5f,
                LineHeight = 1.18f,
                ParagraphSpacingPt = 3f,
                AfterLevel1SpacingPt = 4f,
                AfterHeadingSpacingPt = 2f,
                TableFontSizePt = 7.5f,
                TableCellPaddingPt = 2.25f,
                TableRuleStrokePt = 0.4f,
            },
            Sections = sections,
            Footer = new Footer
            {
                Template = "{page}",
                FontSizePt = 7f,
                IncludeBody = true,
            },
        };
    }

    static TableSection BuildLineTable(InvoiceBase invoice, string currency)
    {
        var rows = invoice.InvoiceLine.Select(line =>
        {
            var name = line.Item.Name?.Value
                ?? line.Item.Description?.FirstOrDefault()?.Value
                ?? "(item)";
            var code = line.Item.SellersItemIdentification?.ID?.Value ?? string.Empty;
            IContent quantity = line.InvoicedQuantity is { } q
                ? new Number(q.Value, NumberFormat.Quantity())
                : new Text("—");
            var unitPrice = line.Price?.PriceAmount is { } price
                ? (IContent)new Number(price.Value, NumberFormat.Currency(currency))
                : new Text("—");
            var vatPercent = LineVatPercent(line);
            var vatAmount = line.TaxTotal?.FirstOrDefault()?.TaxAmount is { } tax
                ? (IContent)new Number(tax.Value, NumberFormat.Currency(currency))
                : new Text("—");
            var amount = new Number(
                line.LineExtensionAmount.Value,
                NumberFormat.Currency(currency));
            return new TableRow(
                [
                    new TableCell(line.ID.Value),
                    new TableCell(name),
                    new TableCell(code),
                    new TableCell(quantity),
                    new TableCell(line.InvoicedQuantity?.unitCode ?? string.Empty),
                    new TableCell(unitPrice),
                    new TableCell(vatPercent),
                    new TableCell(vatAmount),
                    new TableCell(amount),
                ]);
        }).ToArray();

        return new TableSection(
            [
                new TableColumn(new ColumnWidth.Weight(0.04f), HorizontalAlignment.Left),
                new TableColumn(new ColumnWidth.Weight(0.28f), HorizontalAlignment.Left),
                new TableColumn(new ColumnWidth.Weight(0.10f), HorizontalAlignment.Left),
                new TableColumn(new ColumnWidth.Weight(0.07f), HorizontalAlignment.Right),
                new TableColumn(new ColumnWidth.Weight(0.07f), HorizontalAlignment.Center),
                new TableColumn(new ColumnWidth.Weight(0.11f), HorizontalAlignment.Right),
                new TableColumn(new ColumnWidth.Weight(0.08f), HorizontalAlignment.Right),
                new TableColumn(new ColumnWidth.Weight(0.11f), HorizontalAlignment.Right),
                new TableColumn(new ColumnWidth.Weight(0.14f), HorizontalAlignment.Right),
            ],
            rows,
            new TableOptions
            {
                Header = new TableRow(
                [
                    new TableCell("#"),
                    new TableCell("Description"),
                    new TableCell("Item"),
                    new TableCell("Qty"),
                    new TableCell("Unit"),
                    new TableCell("Price"),
                    new TableCell("VAT %"),
                    new TableCell("VAT"),
                    new TableCell($"Amount ({currency})"),
                ]),
                ShowHeader = true,
                RepeatHeader = true,
                HeaderBackground = true,
                RuleStyle = TableRuleStyle.Horizontal,
                CellPaddingPt = 2.25f,
            });
    }

    static IReadOnlyList<IContent> BuildSummaryContents(InvoiceBase invoice, string currency)
    {
        var result = new List<IContent>();
        var monetary = invoice.LegalMonetaryTotal;
        if (monetary.LineExtensionAmount is { } lines)
            result.Add(new KeyValue("Lines", new Number(lines.Value, NumberFormat.Currency(currency))));
        if (monetary.AllowanceTotalAmount is { } allowance)
            result.Add(new KeyValue("Allowances", new Number(allowance.Value, NumberFormat.Currency(currency))));
        if (monetary.ChargeTotalAmount is { } charge)
            result.Add(new KeyValue("Charges", new Number(charge.Value, NumberFormat.Currency(currency))));
        if (monetary.TaxExclusiveAmount is { } net)
            result.Add(new KeyValue("Net", new Number(net.Value, NumberFormat.Currency(currency))));
        foreach (var tax in invoice.TaxTotal ?? [])
        {
            foreach (var subtotal in tax.TaxSubtotal ?? [])
            {
                var label = subtotal.TaxCategory?.Percent?.Value is { } percent
                    ? $"VAT {percent.ToString("0.##", Nb)} %"
                    : "VAT";
                result.Add(new KeyValue(
                    label,
                    new Number(subtotal.TaxAmount.Value, NumberFormat.Currency(currency))));
            }
        }

        if (monetary.PrepaidAmount is { } prepaid)
            result.Add(new KeyValue("Prepaid", new Number(prepaid.Value, NumberFormat.Currency(currency))));
        result.Add(new KeyValue(
            $"Due ({currency})",
            new Number(monetary.PayableAmount.Value, NumberFormat.Currency(currency))));
        return result;
    }

    static string ResolveLogoPath(string? logoPath)
    {
        if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
            return logoPath;
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "TestData", "logo-icon.svg");
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        return string.Empty;
    }

    static string BuildPaymentBody(InvoiceBase invoice, string currency)
    {
        var result = new StringBuilder();
        result.AppendLine($"Amount due: {Money(invoice.LegalMonetaryTotal.PayableAmount.Value, currency)}");
        if (invoice.DueDate is { } due)
            result.AppendLine($"Due date: {FormatDate(due)}");
        else if (invoice.PaymentMeans?.FirstOrDefault()?.PaymentDueDate is { } paymentDue)
            result.AppendLine($"Due date: {FormatDate(paymentDue)}");
        else
            result.AppendLine("Due on receipt");

        foreach (var means in invoice.PaymentMeans ?? [])
        {
            var account = means.PayeeFinancialAccount;
            if (account?.ID?.Value is { Length: > 0 } iban)
                result.AppendLine($"Account / IBAN: {iban}");
            if (account?.Name?.Value is { Length: > 0 } accountName)
                result.AppendLine($"Account name: {accountName}");
            foreach (var paymentId in means.PaymentID ?? [])
            {
                if (!string.IsNullOrWhiteSpace(paymentId.Value))
                    result.AppendLine($"Payment reference: {paymentId.Value}");
            }
        }

        foreach (var terms in invoice.PaymentTerms ?? [])
        {
            foreach (var note in terms.Note ?? [])
            {
                if (!string.IsNullOrWhiteSpace(note.Value))
                    result.AppendLine(note.Value);
            }
        }

        return result.Length == 0 ? "Pay as agreed." : result.ToString().TrimEnd();
    }

    static string? PartyName(PartyBase? party) =>
        party?.PartyName?.FirstOrDefault()?.Name?.Value
        ?? party?.PartyLegalEntity?.FirstOrDefault()?.RegistrationName?.Value;

    static string FormatParty(PartyBase? party)
    {
        if (party is null)
            return "—";
        var result = new StringBuilder();
        Append(result, PartyName(party));
        if (party.PostalAddress is { } address)
        {
            Append(result, JoinNonEmpty(" ", address.StreetName?.Value, address.BuildingNumber?.Value));
            Append(result, address.AdditionalStreetName?.Value);
            Append(result, JoinNonEmpty(" ", address.PostalZone?.Value, address.CityName?.Value));
            Append(result, address.Country?.IdentificationCode?.Value);
        }

        foreach (var tax in party.PartyTaxScheme ?? [])
            Append(result, tax.CompanyID?.Value is { Length: > 0 } vat ? $"Tax ID: {vat}" : null);
        Append(result, party.Contact?.ElectronicMail?.Value);
        return result.Length == 0 ? "—" : result.ToString().TrimEnd();
    }

    static string LineVatPercent(InvoiceLineBase line) =>
        line.Item.ClassifiedTaxCategory?.FirstOrDefault()?.Percent?.Value is { } percent
            ? percent.ToString("0.##", Nb)
            : "—";

    static void Append(StringBuilder builder, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            builder.AppendLine(value.Trim());
    }

    static string? JoinNonEmpty(string separator, params string?[] parts)
    {
        var values = parts
            .Where(static part => !string.IsNullOrWhiteSpace(part))
            .Select(static part => part!.Trim())
            .ToArray();
        return values.Length == 0 ? null : string.Join(separator, values);
    }

    static string FormatDate(DateTime date) => date.ToString("dd.MM.yyyy", Nb);

    static string Money(decimal amount, string currency) =>
        $"{amount.ToString("N2", Nb)} {currency}";
}
