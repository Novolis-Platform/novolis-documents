using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Novolis.Documents.Layout;

namespace Novolis.Documents.Skia;

/// <summary>
/// Appends a PDF outline tree for the contents page and level-1 headings.
/// SkiaSharp does not write <c>/Outlines</c>, so this is an incremental update the Novolis PDF parser follows via <c>/Prev</c>.
/// </summary>
static class PdfOutlineWriter
{
    static readonly Regex PagesRef = new(@"/Pages\s+(\d+)\s+\d+\s+R", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    static readonly Regex KidsRefs = new(@"(\d+)\s+\d+\s+R", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    static readonly Regex MediaBox = new(
        @"/MediaBox\s*\[\s*([0-9.+-]+)\s+([0-9.+-]+)\s+([0-9.+-]+)\s+([0-9.+-]+)\s*\]",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);
    static readonly Regex TypePage = new(@"/Type\s*/Page\b", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    static readonly Regex TypePages = new(@"/Type\s*/Pages\b", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static byte[] Append(byte[] pdf, PagePlan plan)
    {
        ArgumentNullException.ThrowIfNull(pdf);
        ArgumentNullException.ThrowIfNull(plan);

        var marks = Collect(plan);
        if (marks.Count == 0)
            return pdf;

        if (!TryReadStructure(pdf, out var structure))
            throw new InvalidOperationException("PDF outline bookmarks could not be written.");

        foreach (var mark in marks)
        {
            if (mark.PageIndex < 0 || mark.PageIndex >= structure.Pages.Count)
                throw new InvalidOperationException("PDF outline bookmarks could not be written.");
        }

        return WriteUpdate(pdf, structure, marks);
    }

    static List<(string Title, int PageIndex)> Collect(PagePlan plan)
    {
        var marks = new List<(string Title, int PageIndex)>();
        foreach (var page in plan.Pages)
        {
            if (page.Kind != PageKind.Toc)
                continue;
            marks.Add(("Contents", page.Number - 1));
            break;
        }

        foreach (var entry in plan.TocEntries)
        {
            if (string.IsNullOrWhiteSpace(entry.Title) || entry.PageNumber < 1)
                continue;
            marks.Add((entry.Title.Trim(), entry.PageNumber - 1));
        }

        return marks;
    }

    static byte[] WriteUpdate(byte[] pdf, PdfStructure structure, List<(string Title, int PageIndex)> marks)
    {
        var next = structure.Size;
        var itemIds = new int[marks.Count];
        for (var i = 0; i < marks.Count; i++)
            itemIds[i] = next++;
        var outlineId = next++;
        var catalogId = next++;

        var appended = new StringBuilder();
        if (pdf.Length == 0 || pdf[^1] != (byte)'\n')
            appended.Append('\n');

        var offsets = new List<(int Id, int Offset)>();
        var baseLength = pdf.Length + (pdf.Length == 0 || pdf[^1] == (byte)'\n' ? 0 : 1);

        void AddObject(int id, string body)
        {
            var offset = baseLength + Encoding.ASCII.GetByteCount(appended.ToString());
            offsets.Add((id, offset));
            appended.Append(id.ToString(CultureInfo.InvariantCulture));
            appended.Append(" 0 obj\n");
            appended.Append(body);
            if (!body.EndsWith('\n'))
                appended.Append('\n');
            appended.Append("endobj\n");
        }

        for (var i = 0; i < marks.Count; i++)
        {
            var page = structure.Pages[marks[i].PageIndex];
            var height = page.Height.ToString("0.###", CultureInfo.InvariantCulture);
            var body = new StringBuilder();
            body.Append("<< /Title ").Append(Utf16Hex(marks[i].Title)).Append('\n');
            body.Append("/Parent ").Append(outlineId).Append(" 0 R\n");
            if (i > 0)
                body.Append("/Prev ").Append(itemIds[i - 1]).Append(" 0 R\n");
            if (i + 1 < marks.Count)
                body.Append("/Next ").Append(itemIds[i + 1]).Append(" 0 R\n");
            body.Append("/Dest [").Append(page.ObjectNumber).Append(" 0 R /XYZ 0 ").Append(height).Append(" null]\n>>");
            AddObject(itemIds[i], body.ToString());
        }

        AddObject(
            outlineId,
            "<< /Type /Outlines\n/First "
            + itemIds[0].ToString(CultureInfo.InvariantCulture)
            + " 0 R\n/Last "
            + itemIds[^1].ToString(CultureInfo.InvariantCulture)
            + " 0 R\n/Count "
            + marks.Count.ToString(CultureInfo.InvariantCulture)
            + " >>");

        var catalog = structure.CatalogDictionary.TrimEnd();
        if (!catalog.EndsWith(">>", StringComparison.Ordinal))
            throw new InvalidOperationException("PDF outline bookmarks could not be written.");
        var updatedCatalog = catalog[..^2] + "/Outlines " + outlineId.ToString(CultureInfo.InvariantCulture) + " 0 R >>";
        AddObject(catalogId, updatedCatalog);

        var xrefAt = baseLength + Encoding.ASCII.GetByteCount(appended.ToString());
        appended.Append("xref\n");
        appended.Append(itemIds[0].ToString(CultureInfo.InvariantCulture));
        appended.Append(' ');
        appended.Append((catalogId - itemIds[0] + 1).ToString(CultureInfo.InvariantCulture));
        appended.Append('\n');
        foreach (var (_, offset) in offsets)
        {
            appended.Append(offset.ToString("D10", CultureInfo.InvariantCulture));
            appended.Append(" 00000 n \n");
        }

        appended.Append("trailer\n<< /Size ");
        appended.Append(next.ToString(CultureInfo.InvariantCulture));
        appended.Append("\n/Root ");
        appended.Append(catalogId.ToString(CultureInfo.InvariantCulture));
        appended.Append(" 0 R\n");
        if (structure.Info > 0)
        {
            appended.Append("/Info ");
            appended.Append(structure.Info.ToString(CultureInfo.InvariantCulture));
            appended.Append(" 0 R\n");
        }

        appended.Append("/Prev ");
        appended.Append(structure.StartXref.ToString(CultureInfo.InvariantCulture));
        appended.Append(" >>\nstartxref\n");
        appended.Append(xrefAt.ToString(CultureInfo.InvariantCulture));
        appended.Append("\n%%EOF\n");

        var tail = Encoding.ASCII.GetBytes(appended.ToString());
        var result = new byte[pdf.Length + tail.Length];
        Buffer.BlockCopy(pdf, 0, result, 0, pdf.Length);
        Buffer.BlockCopy(tail, 0, result, pdf.Length, tail.Length);
        return result;
    }

    static string Utf16Hex(string text)
    {
        var bytes = Encoding.BigEndianUnicode.GetBytes(text);
        var hex = new StringBuilder(bytes.Length * 2 + 6);
        hex.Append("<FEFF");
        foreach (var b in bytes)
            hex.Append(b.ToString("X2", CultureInfo.InvariantCulture));
        hex.Append('>');
        return hex.ToString();
    }

    static bool TryReadStructure(byte[] pdf, out PdfStructure structure)
    {
        structure = default;
        var startToken = LastIndexOf(pdf, "startxref"u8);
        if (startToken < 0 || !TryReadInt(pdf, startToken + "startxref".Length, out var startXref, out _))
            return false;
        if (startXref < 0 || startXref >= pdf.Length)
            return false;
        if (!StartsWith(pdf, startXref, "xref"u8))
            return false;

        var offsets = new Dictionary<int, int>();
        var position = startXref + "xref".Length;
        while (position < pdf.Length)
        {
            position = SkipWhitespace(pdf, position);
            if (StartsWith(pdf, position, "trailer"u8))
                break;
            if (!TryReadInt(pdf, position, out var startId, out position))
                return false;
            if (!TryReadInt(pdf, position, out var count, out position))
                return false;
            for (var i = 0; i < count; i++)
            {
                position = SkipWhitespace(pdf, position);
                if (position + 18 > pdf.Length)
                    return false;
                if (!TryReadInt(pdf, position, out var offset, out var afterOffset))
                    return false;
                if (!TryReadInt(pdf, afterOffset, out var generation, out var afterGen))
                    return false;
                afterGen = SkipWhitespace(pdf, afterGen);
                if (afterGen >= pdf.Length)
                    return false;
                var inUse = pdf[afterGen] == (byte)'n';
                if (inUse && generation == 0)
                    offsets[startId + i] = offset;
                position = afterGen + 1;
            }
        }

        position = SkipWhitespace(pdf, position);
        if (!StartsWith(pdf, position, "trailer"u8))
            return false;
        var trailer = ReadDictionary(pdf, position);
        if (trailer is null)
            return false;
        if (!TryMatch(trailer, @"/Size\s+(\d+)", out var sizeText) || !int.TryParse(sizeText, out var size))
            return false;
        if (!TryMatch(trailer, @"/Root\s+(\d+)\s+\d+\s+R", out var rootText) || !int.TryParse(rootText, out var root))
            return false;
        var info = 0;
        if (TryMatch(trailer, @"/Info\s+(\d+)\s+\d+\s+R", out var infoText))
            int.TryParse(infoText, out info);
        if (!offsets.TryGetValue(root, out var catalogOffset))
            return false;

        var catalogObject = ReadObject(pdf, catalogOffset);
        var catalogDict = ExtractDictionary(catalogObject);
        if (catalogDict is null || !PagesRef.IsMatch(catalogDict))
            return false;
        var pagesId = int.Parse(PagesRef.Match(catalogDict).Groups[1].Value, CultureInfo.InvariantCulture);
        var pages = new List<PdfPageRef>();
        if (!CollectPages(pdf, offsets, pagesId, pages, []))
            return false;
        if (pages.Count == 0)
            return false;

        structure = new PdfStructure(size, root, info, startXref, catalogDict, pages);
        return true;
    }

    static bool CollectPages(
        byte[] pdf,
        Dictionary<int, int> offsets,
        int objectNumber,
        List<PdfPageRef> pages,
        HashSet<int> visited)
    {
        if (!visited.Add(objectNumber) || !offsets.TryGetValue(objectNumber, out var offset))
            return false;
        var dict = ExtractDictionary(ReadObject(pdf, offset));
        if (dict is null)
            return false;
        if (TypePage.IsMatch(dict) && !TypePages.IsMatch(dict))
        {
            var height = 792f;
            var box = MediaBox.Match(dict);
            if (box.Success && float.TryParse(box.Groups[4].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var top))
                height = top;
            pages.Add(new PdfPageRef(objectNumber, height));
            return true;
        }

        var kids = dict.IndexOf("/Kids", StringComparison.Ordinal);
        if (kids < 0)
            return false;
        var open = dict.IndexOf('[', kids);
        var close = open < 0 ? -1 : dict.IndexOf(']', open);
        if (open < 0 || close < 0)
            return false;
        foreach (Match match in KidsRefs.Matches(dict[open..close]))
        {
            var child = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            if (!CollectPages(pdf, offsets, child, pages, visited))
                return false;
        }

        return true;
    }

    static string ReadObject(byte[] pdf, int offset)
    {
        var end = IndexOf(pdf, "endobj"u8, offset);
        if (end < 0)
            return string.Empty;
        return Encoding.Latin1.GetString(pdf, offset, end - offset);
    }

    static string? ExtractDictionary(string text)
    {
        var start = text.IndexOf("<<", StringComparison.Ordinal);
        if (start < 0)
            return null;
        var depth = 0;
        for (var i = start; i < text.Length - 1; i++)
        {
            if (text[i] == '<' && text[i + 1] == '<')
            {
                depth++;
                i++;
                continue;
            }

            if (text[i] == '>' && text[i + 1] == '>')
            {
                depth--;
                if (depth == 0)
                    return text[start..(i + 2)];
                i++;
            }
        }

        return null;
    }

    static string? ReadDictionary(byte[] pdf, int start)
    {
        var text = Encoding.Latin1.GetString(pdf, start, pdf.Length - start);
        return ExtractDictionary(text);
    }

    static bool TryMatch(string text, string pattern, out string value)
    {
        var match = Regex.Match(text, pattern, RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            value = string.Empty;
            return false;
        }

        value = match.Groups[1].Value;
        return true;
    }

    static int SkipWhitespace(byte[] pdf, int position)
    {
        while (position < pdf.Length && pdf[position] is (byte)' ' or (byte)'\n' or (byte)'\r' or (byte)'\t')
            position++;
        return position;
    }

    static bool TryReadInt(byte[] pdf, int position, out int value, out int next)
    {
        position = SkipWhitespace(pdf, position);
        var start = position;
        while (position < pdf.Length && pdf[position] is >= (byte)'0' and <= (byte)'9')
            position++;
        if (position == start)
        {
            value = 0;
            next = position;
            return false;
        }

        value = int.Parse(Encoding.ASCII.GetString(pdf, start, position - start), CultureInfo.InvariantCulture);
        next = position;
        return true;
    }

    static bool StartsWith(byte[] pdf, int position, ReadOnlySpan<byte> token) =>
        position >= 0 && position + token.Length <= pdf.Length && pdf.AsSpan(position, token.Length).SequenceEqual(token);

    static int LastIndexOf(byte[] pdf, ReadOnlySpan<byte> token)
    {
        for (var i = pdf.Length - token.Length; i >= 0; i--)
        {
            if (pdf.AsSpan(i, token.Length).SequenceEqual(token))
                return i;
        }

        return -1;
    }

    static int IndexOf(byte[] pdf, ReadOnlySpan<byte> token, int start)
    {
        for (var i = System.Math.Max(0, start); i <= pdf.Length - token.Length; i++)
        {
            if (pdf.AsSpan(i, token.Length).SequenceEqual(token))
                return i;
        }

        return -1;
    }

    readonly record struct PdfPageRef(int ObjectNumber, float Height);

    readonly record struct PdfStructure(
        int Size,
        int Root,
        int Info,
        int StartXref,
        string CatalogDictionary,
        List<PdfPageRef> Pages);
}
