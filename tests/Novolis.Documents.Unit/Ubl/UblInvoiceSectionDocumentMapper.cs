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
        var isCreditNote = string.Equals(
            invoice.InvoiceTypeCode?.Value,
            "381",
            StringComparison.OrdinalIgnoreCase);
        var supplierName = PartyName(invoice.AccountingSupplierParty.Party) ?? "Supplier";
        var customerName = PartyName(invoice.AccountingCustomerParty.Party) ?? "Customer";
        var sections = new List<ISection>();
        var logo = ResolveLogoPath(logoPath);

        var titleContents = new List<IContent>
        {
            new Heading(isCreditNote ? "CREDIT NOTE" : "INVOICE", 2),
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

        if (invoice.PayeeParty is { } payee)
            sections.Add(new Section(
                [
                    new Heading("Payee", 3),
                    new Text(FormatParty(payee)),
                ]));

        if (invoice.TaxRepresentativeParty is { } taxRepresentative)
            sections.Add(new Section(
                [
                    new Heading("Tax representative", 3),
                    new Text(FormatParty(taxRepresentative)),
                ]));

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

        AddReferenceSections(sections, invoice);
        AddDeliverySection(sections, invoice);

        foreach (var note in invoice.Note ?? [])
        {
            if (!string.IsNullOrWhiteSpace(note.Value))
                sections.Add(new Section([new Text(note.Value)]));
        }

        sections.Add(BuildLineTable(invoice, currency));
        AddAdjustmentSection(sections, invoice, currency);
        AddTaxSection(sections, invoice, currency);

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

        AddSupportingDocumentSection(sections, invoice);

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
            var code = ItemCodes(line);
            IContent quantity = line.InvoicedQuantity is { } q
                ? new Number(q.Value, NumberFormat.Quantity())
                : new Text("—");
            var unitPrice = line.Price?.PriceAmount is { } price
                ? (IContent)new Number(price.Value, NumberFormat.Currency(currency))
                : new Text("—");
            IContent vatPercent = line.Item.ClassifiedTaxCategory?.FirstOrDefault()?.Percent?.Value is { } percent
                ? new Number(percent, NumberFormat.Percentage(2))
                : new Text("—");
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
        if (monetary.TaxInclusiveAmount is { } gross)
            result.Add(new KeyValue("Gross", new Number(gross.Value, NumberFormat.Currency(currency))));
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
        if (monetary.PayableRoundingAmount is { } rounding)
            result.Add(new KeyValue("Rounding", new Number(rounding.Value, NumberFormat.Currency(currency))));
        result.Add(new KeyValue(
            $"Due ({currency})",
            new Number(monetary.PayableAmount.Value, NumberFormat.Currency(currency))));
        return result;
    }

    static void AddReferenceSections(List<ISection> sections, InvoiceBase invoice)
    {
        var references = new List<IContent>();
        AppendDocumentReference(references, "Additional", invoice.AdditionalDocumentReference);
        AppendDocumentReference(references, "Despatch", invoice.DespatchDocumentReference);
        AppendDocumentReference(references, "Receipt", invoice.ReceiptDocumentReference);
        AppendDocumentReference(references, "Statement", invoice.StatementDocumentReference);
        AppendDocumentReference(references, "Contract", invoice.ContractDocumentReference);

        foreach (var billing in invoice.BillingReference ?? [])
        {
            AppendDocumentReference(references, "Previous invoice", billing.InvoiceDocumentReference);
            AppendDocumentReference(references, "Credit note", billing.CreditNoteDocumentReference);
            AppendDocumentReference(references, "Debit note", billing.DebitNoteDocumentReference);
        }

        foreach (var project in invoice.ProjectReference ?? [])
        {
            if (!string.IsNullOrWhiteSpace(project.ID.Value))
                references.Add(new KeyValue("Project", project.ID.Value));
        }

        if (references.Count > 0)
        {
            references.Insert(0, new Heading("References", 3));
            sections.Add(new Section(references));
        }
    }

    static void AppendDocumentReference(
        List<IContent> target,
        string label,
        IEnumerable<DocumentReferenceBase>? references)
    {
        foreach (var reference in references ?? [])
            AppendDocumentReference(target, label, reference);
    }

    static void AppendDocumentReference(
        List<IContent> target,
        string label,
        DocumentReferenceBase? reference)
    {
        if (reference is null)
            return;
        if (!string.IsNullOrWhiteSpace(reference.ID.Value))
            target.Add(new KeyValue(label, reference.ID.Value));
        foreach (var description in reference.DocumentDescription ?? [])
        {
            if (!string.IsNullOrWhiteSpace(description.Value))
                target.Add(new KeyValue($"{label} note", description.Value));
        }

        var external = reference.Attachment?.ExternalReference;
        if (!string.IsNullOrWhiteSpace(external?.FileName?.Value))
            target.Add(new KeyValue($"{label} file", external.FileName.Value));
        if (!string.IsNullOrWhiteSpace(external?.URI?.Value))
            target.Add(new KeyValue($"{label} URL", external.URI.Value));
    }

    static void AddDeliverySection(List<ISection> sections, InvoiceBase invoice)
    {
        var deliveries = invoice.Delivery ?? [];
        if (deliveries.Count == 0)
            return;

        var contents = new List<IContent> { new Heading("Delivery", 3) };
        foreach (var delivery in deliveries)
        {
            if (!string.IsNullOrWhiteSpace(delivery.ID?.Value))
                contents.Add(new KeyValue("Delivery", delivery.ID.Value));
            if (!string.IsNullOrWhiteSpace(delivery.TrackingID?.Value))
                contents.Add(new KeyValue("Tracking", delivery.TrackingID.Value));
            if (delivery.ActualDeliveryDate is { } actual)
                contents.Add(new KeyValue("Delivered", FormatDate(actual)));
            if (delivery.LatestDeliveryDate is { } latest)
                contents.Add(new KeyValue("Latest", FormatDate(latest)));
            if (delivery.DeliveryAddress is { } address)
                contents.Add(new Text(FormatAddress(address)));
        }

        sections.Add(new Section(contents));
    }

    static void AddAdjustmentSection(
        List<ISection> sections,
        InvoiceBase invoice,
        string currency)
    {
        var adjustments = invoice.AllowanceCharge ?? [];
        if (adjustments.Count == 0)
            return;

        var rows = adjustments.Select(adjustment => new TableRow(
            new TableCell(adjustment.ChargeIndicator ? "Charge" : "Allowance"),
            new TableCell(
                adjustment.AllowanceChargeReason?.FirstOrDefault()?.Value
                ?? adjustment.AllowanceChargeReasonCode?.Value
                ?? string.Empty),
            new TableCell(new Number(
                adjustment.Amount.Value,
                NumberFormat.Currency(adjustment.Amount.currencyID ?? currency)))))
            .ToArray();
        sections.Add(new TableSection(
            [
                new TableColumn(new ColumnWidth.Weight(0.22f)),
                new TableColumn(new ColumnWidth.Weight(0.53f)),
                new TableColumn(new ColumnWidth.Weight(0.25f), HorizontalAlignment.Right),
            ],
            rows,
            new TableOptions
            {
                Header = new TableRow("Type", "Reason", $"Amount ({currency})"),
                ShowHeader = true,
                RepeatHeader = true,
                HeaderBackground = true,
                RuleStyle = TableRuleStyle.Horizontal,
            }));
    }

    static void AddTaxSection(
        List<ISection> sections,
        InvoiceBase invoice,
        string currency)
    {
        var rows = new List<TableRow>();
        foreach (var taxTotal in invoice.TaxTotal ?? [])
        {
            foreach (var subtotal in taxTotal.TaxSubtotal ?? [])
            {
                var category = subtotal.TaxCategory?.Name?.Value
                    ?? subtotal.TaxCategory?.ID?.Value
                    ?? "VAT";
                var rate = subtotal.Percent?.Value is { } percent
                    ? new Number(percent, NumberFormat.Percentage(2))
                    : (IContent)new Text("—");
                var taxable = subtotal.TaxableAmount is { } taxableAmount
                    ? (IContent)new Number(
                        taxableAmount.Value,
                        NumberFormat.Currency(taxableAmount.currencyID ?? currency))
                    : new Text("—");
                rows.Add(new TableRow(
                    new TableCell(category),
                    new TableCell(taxable),
                    new TableCell(rate),
                    new TableCell(new Number(
                        subtotal.TaxAmount.Value,
                        NumberFormat.Currency(subtotal.TaxAmount.currencyID ?? currency)))));
            }

            if ((taxTotal.TaxSubtotal is null || taxTotal.TaxSubtotal.Count == 0)
                && taxTotal.TaxAmount is { } total)
            {
                rows.Add(new TableRow(
                    new TableCell("Tax total"),
                    new TableCell(string.Empty),
                    new TableCell(string.Empty),
                    new TableCell(new Number(
                        total.Value,
                        NumberFormat.Currency(total.currencyID ?? currency)))));
            }
        }

        if (rows.Count == 0)
            return;

        sections.Add(new TableSection(
            [
                new TableColumn(new ColumnWidth.Weight(0.28f)),
                new TableColumn(new ColumnWidth.Weight(0.28f), HorizontalAlignment.Right),
                new TableColumn(new ColumnWidth.Weight(0.18f), HorizontalAlignment.Right),
                new TableColumn(new ColumnWidth.Weight(0.26f), HorizontalAlignment.Right),
            ],
            rows,
            new TableOptions
            {
                Header = new TableRow("Category", "Taxable", "Rate", $"Tax ({currency})"),
                ShowHeader = true,
                RepeatHeader = true,
                HeaderBackground = true,
                RuleStyle = TableRuleStyle.Horizontal,
            }));
    }

    static void AddSupportingDocumentSection(List<ISection> sections, InvoiceBase invoice)
    {
        var references = invoice.AdditionalDocumentReference ?? [];
        if (references.Count == 0)
            return;

        var contents = new List<IContent> { new Heading("Supporting documents", 3) };
        foreach (var reference in references)
        {
            AppendDocumentReference(contents, "Document", reference);
        }

        if (contents.Count > 1)
            sections.Add(new Section(contents));
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

    static string ItemCodes(InvoiceLineBase line)
    {
        var codes = new[]
        {
            line.Item.SellersItemIdentification?.ID?.Value,
            line.Item.BuyersItemIdentification?.ID?.Value,
            line.Item.StandardItemIdentification?.ID?.Value,
        };
        return string.Join(
            " · ",
            codes.Where(static code => !string.IsNullOrWhiteSpace(code)));
    }

    static string FormatParty(PartyBase? party)
    {
        if (party is null)
            return "—";
        var result = new StringBuilder();
        Append(result, PartyName(party));
        Append(result, party.EndpointID?.Value);
        foreach (var identification in party.PartyIdentification ?? [])
            Append(result, identification.ID.Value);
        if (party.PostalAddress is { } address)
            Append(result, FormatAddress(address));

        foreach (var tax in party.PartyTaxScheme ?? [])
            Append(result, tax.CompanyID?.Value is { Length: > 0 } vat ? $"Tax ID: {vat}" : null);
        Append(result, party.Contact?.Name?.Value);
        Append(result, party.Contact?.Telephone?.Value);
        Append(result, party.Contact?.ElectronicMail?.Value);
        return result.Length == 0 ? "—" : result.ToString().TrimEnd();
    }

    static string FormatAddress(AddressBase address)
    {
        var lines = new List<string>();
        Append(lines, JoinNonEmpty(" ", address.StreetName?.Value, address.BuildingNumber?.Value));
        Append(lines, address.AdditionalStreetName?.Value);
        Append(lines, JoinNonEmpty(" ", address.PostalZone?.Value, address.CityName?.Value));
        Append(lines, address.Country?.Name?.Value ?? address.Country?.IdentificationCode?.Value);
        return lines.Count == 0 ? "—" : string.Join(Environment.NewLine, lines);
    }

    static void Append(StringBuilder builder, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            builder.AppendLine(value.Trim());
    }

    static void Append(List<string> lines, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            lines.Add(value.Trim());
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
