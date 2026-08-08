using System.Globalization;
using System.Text;
using Novolis.Documents;
using Novolis.Math.Measure;
using Novolis.Xsd.Ubl.Lean;

namespace Novolis.Documents.Unit.Ubl;

/// <summary>
/// Experimental mapper: UBL Lean invoice → <see cref="PagedDocument"/> using a customary
/// Norwegian invoice <em>layout</em> (parties → lines → payment; A4; no giro), English copy.
/// Not a product API.
/// </summary>
internal static class UblInvoiceDocumentMapper
{
    /// <summary>Norwegian number/date shapes (dd.MM.yyyy, 1 234,56) — not Norwegian words.</summary>
    static readonly CultureInfo Nb = CultureInfo.GetCultureInfo("nb-NO");

    public static PagedDocument FromLean(InvoiceBase invoice, string? logoPath = null)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        var currency = invoice.DocumentCurrencyCode?.Value
            ?? invoice.LegalMonetaryTotal.PayableAmount.currencyID
            ?? "EUR";
        var invoiceId = invoice.ID.Value;
        var supplierName = PartyName(invoice.AccountingSupplierParty.Party) ?? "Supplier";
        var customerName = PartyName(invoice.AccountingCustomerParty.Party) ?? "Customer";
        var logo = ResolveLogoPath(logoPath);

        var blocks = new List<IBlock>();

        // Letterhead: logo + title | compact meta (2 columns only)
        var titleStack = new List<IBlock>
        {
            new HeadingBlock { Level = 2, Text = "INVOICE" },
            new ParagraphBlock { Text = supplierName },
        };

        var metaTable = new TableBlock
        {
            Headers = [],
            Rows =
            [
                ["No.", invoiceId],
                ["Issued", FormatDate(invoice.IssueDate)],
                [
                    "Due",
                    invoice.DueDate is { } due ? FormatDate(due) : "On receipt",
                ],
                ["Currency", currency],
            ],
            ColumnWidths = [0.38f, 0.62f],
            ColumnAlignments = [CellAlign.Left, CellAlign.Right],
            ShowHeader = false,
            RuleStyle = TableRuleStyle.None,
            HeaderBackground = false,
        };

        if (!string.IsNullOrWhiteSpace(logo) && File.Exists(logo))
        {
            blocks.Add(new ColumnsBlock
            {
                GapPt = 12f,
                Fractions = [0.10f, 0.48f, 0.42f],
                Columns =
                [
                    [new ImageBlock { Path = logo, WidthPt = 40f, HeightPt = 40f }],
                    titleStack,
                    [metaTable],
                ],
            });
        }
        else
        {
            blocks.Add(new ColumnsBlock
            {
                GapPt = 16f,
                Fractions = [0.55f, 0.45f],
                Columns = [titleStack, [metaTable]],
            });
        }

        // Where: seller | buyer
        blocks.Add(new ColumnsBlock
        {
            GapPt = 20f,
            Fractions = [0.5f, 0.5f],
            Columns =
            [
                [
                    new HeadingBlock { Level = 3, Text = "Supplier" },
                    new ParagraphBlock { Text = FormatParty(invoice.AccountingSupplierParty.Party) },
                ],
                [
                    new HeadingBlock { Level = 3, Text = "Bill to" },
                    new ParagraphBlock { Text = FormatParty(invoice.AccountingCustomerParty.Party) },
                ],
            ],
        });

        var refs = new List<string>();
        if (invoice.OrderReference?.ID is { Value: { Length: > 0 } orderId })
            refs.Add($"Order {orderId}");
        if (invoice.InvoicePeriod is { Count: > 0 } periods)
        {
            var p = periods[0];
            var start = p.StartDate is { } s ? FormatDate(s) : "…";
            var end = p.EndDate is { } e ? FormatDate(e) : "…";
            refs.Add($"Period {start} – {end}");
        }

        if (refs.Count > 0)
            blocks.Add(new ParagraphBlock { Text = string.Join("  ·  ", refs) });

        foreach (var note in invoice.Note ?? [])
        {
            if (!string.IsNullOrWhiteSpace(note.Value))
                blocks.Add(new ParagraphBlock { Text = note.Value });
        }

        // What: wide line table
        blocks.Add(BuildLineTable(invoice, currency));

        // How + summary side by side (summary stays 2 columns)
        blocks.Add(new ColumnsBlock
        {
            GapPt = 22f,
            Fractions = [0.58f, 0.42f],
            Columns =
            [
                [
                    new HeadingBlock { Level = 3, Text = "Payment" },
                    new ParagraphBlock { Text = BuildPaymentBody(invoice, currency) },
                ],
                [BuildSummaryTable(invoice, currency)],
            ],
        });

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
            IncludeCover = false,
            IncludeToc = false,
            First = null,
            Last = null,
            Header = null,
            Footer = new Footer
            {
                Template = "{page}",
                FontSizePt = 7f,
                IncludeFirstPage = false,
                IncludeToc = false,
                IncludeBody = true,
                IncludeLastPage = false,
            },
            Body = blocks,
        };
    }

    static string ResolveLogoPath(string? logoPath)
    {
        if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
            return logoPath;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            foreach (var candidate in new[]
                     {
                         Path.Combine(dir.FullName, "TestData", "logo-icon.svg"),
                         Path.Combine(dir.FullName, "tests", "Novolis.Documents.Unit", "TestData", "logo-icon.svg"),
                         Path.Combine(dir.FullName, ".github", "brand", "logo-icon.svg"),
                         Path.Combine(dir.FullName, "logo-icon.svg"),
                     })
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            dir = dir.Parent;
        }

        return string.Empty;
    }

    static TableBlock BuildLineTable(InvoiceBase invoice, string currency)
    {
        var rows = new List<IReadOnlyList<string>>();
        foreach (var line in invoice.InvoiceLine)
        {
            var name = line.Item.Name?.Value
                ?? line.Item.Description?.FirstOrDefault()?.Value
                ?? "(item)";
            var code = line.Item.SellersItemIdentification?.ID?.Value ?? "";
            var qty = line.InvoicedQuantity is { } q
                ? q.Value.ToString("0.##", Nb)
                : "—";
            var unit = line.InvoicedQuantity is { unitCode: { Length: > 0 } u } ? u : "";
            var unitPrice = line.Price?.PriceAmount is { } pa
                ? Amount(pa.Value)
                : "—";
            var vatPct = LineVatPercent(line);
            var vatAmt = line.TaxTotal?.FirstOrDefault()?.TaxAmount is { } ta
                ? Amount(ta.Value)
                : "—";
            var ext = Amount(line.LineExtensionAmount.Value);
            rows.Add([line.ID.Value, name, code, qty, unit, unitPrice, vatPct, vatAmt, ext]);
        }

        return new TableBlock
        {
            Headers =
            [
                "#", "Description", "Item", "Qty", "Unit", "Price", "VAT %", "VAT", $"Amount ({currency})",
            ],
            Rows = rows,
            ColumnWidths = [0.04f, 0.28f, 0.10f, 0.07f, 0.07f, 0.11f, 0.08f, 0.11f, 0.14f],
            ColumnAlignments =
            [
                CellAlign.Left, CellAlign.Left, CellAlign.Left,
                CellAlign.Right, CellAlign.Center, CellAlign.Right,
                CellAlign.Right, CellAlign.Right, CellAlign.Right,
            ],
            ShowHeader = true,
            RuleStyle = TableRuleStyle.Horizontal,
            HeaderBackground = true,
            RepeatHeaderOnPageBreak = true,
        };
    }

    static TableBlock BuildSummaryTable(InvoiceBase invoice, string currency)
    {
        var m = invoice.LegalMonetaryTotal;
        var rows = new List<IReadOnlyList<string>>();
        if (m.LineExtensionAmount is { } lea)
            rows.Add(["Lines", Amount(lea.Value)]);
        if (m.AllowanceTotalAmount is { } ata)
            rows.Add(["Allowances", Amount(ata.Value)]);
        if (m.ChargeTotalAmount is { } cta)
            rows.Add(["Charges", Amount(cta.Value)]);
        if (m.TaxExclusiveAmount is { } tea)
            rows.Add(["Net", Amount(tea.Value)]);

        foreach (var tax in invoice.TaxTotal ?? [])
        {
            foreach (var sub in tax.TaxSubtotal ?? [])
            {
                var pct = sub.TaxCategory?.Percent?.Value;
                var label = pct is { } p ? $"VAT {p.ToString("0.##", Nb)} %" : "VAT";
                rows.Add([label, Amount(sub.TaxAmount.Value)]);
            }
        }

        if (m.PrepaidAmount is { } ppa)
            rows.Add(["Prepaid", Amount(ppa.Value)]);
        rows.Add([$"Due ({currency})", Amount(m.PayableAmount.Value)]);

        return new TableBlock
        {
            Headers = ["", ""],
            Rows = rows,
            ColumnWidths = [0.58f, 0.42f],
            ColumnAlignments = [CellAlign.Left, CellAlign.Right],
            ShowHeader = false,
            RuleStyle = TableRuleStyle.Horizontal,
            HeaderBackground = false,
        };
    }

    static string LineVatPercent(InvoiceLineBase line)
    {
        var pct = line.Item.ClassifiedTaxCategory?.FirstOrDefault()?.Percent?.Value;
        return pct is { } p ? p.ToString("0.##", Nb) : "—";
    }

    static string BuildPaymentBody(InvoiceBase invoice, string currency)
    {
        var sb = new StringBuilder();
        var payable = Money(
            invoice.LegalMonetaryTotal.PayableAmount.Value,
            invoice.LegalMonetaryTotal.PayableAmount.currencyID ?? currency);
        sb.AppendLine($"Amount due: {payable}");

        if (invoice.DueDate is { } due)
            sb.AppendLine($"Due date: {FormatDate(due)}");
        else if (invoice.PaymentMeans?.FirstOrDefault()?.PaymentDueDate is { } payDue)
            sb.AppendLine($"Due date: {FormatDate(payDue)}");
        else
            sb.AppendLine("Due on receipt");

        foreach (var means in invoice.PaymentMeans ?? [])
        {
            var account = means.PayeeFinancialAccount;
            if (account?.ID?.Value is { Length: > 0 } iban)
                sb.AppendLine($"Account / IBAN: {iban}");
            if (account?.Name?.Value is { Length: > 0 } accName)
                sb.AppendLine($"Account name: {accName}");
            foreach (var kid in means.PaymentID ?? [])
            {
                if (!string.IsNullOrWhiteSpace(kid.Value))
                    sb.AppendLine($"Payment reference: {kid.Value}");
            }
        }

        foreach (var terms in invoice.PaymentTerms ?? [])
        {
            foreach (var note in terms.Note ?? [])
            {
                if (!string.IsNullOrWhiteSpace(note.Value))
                    sb.AppendLine(note.Value);
            }
        }

        if (sb.Length == 0)
            sb.Append("Pay as agreed.");

        return sb.ToString().TrimEnd();
    }

    static string? PartyName(PartyBase? party) =>
        party?.PartyName?.FirstOrDefault()?.Name?.Value
        ?? party?.PartyLegalEntity?.FirstOrDefault()?.RegistrationName?.Value;

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
            Append(sb, JoinNonEmpty(" ", addr.PostalZone?.Value, addr.CityName?.Value));
            Append(sb, addr.Country?.IdentificationCode?.Value);
        }

        foreach (var tax in party.PartyTaxScheme ?? [])
        {
            if (tax.CompanyID?.Value is { Length: > 0 } vat)
                Append(sb, $"Tax ID: {vat}");
        }

        if (party.Contact?.ElectronicMail?.Value is { Length: > 0 } mail)
            Append(sb, mail);

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

    static string FormatDate(DateTime date) =>
        date.ToString("dd.MM.yyyy", Nb);

    static string Amount(decimal amount) =>
        amount.ToString("N2", Nb);

    static string Money(decimal amount, string currency) =>
        $"{Amount(amount)} {currency}";
}
