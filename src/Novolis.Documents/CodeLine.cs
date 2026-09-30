namespace Novolis.Documents;

/// <summary>One source line as ordered spans (plain or highlighted).</summary>
public sealed class CodeLine
{
    /// <summary>Runs left-to-right.</summary>
    public IReadOnlyList<CodeSpan> Spans { get; init; } = [];

    /// <summary>Concatenated span text.</summary>
    public string PlainText
    {
        get
        {
            if (Spans.Count == 0)
                return string.Empty;
            if (Spans.Count == 1)
                return Spans[0].Text;
            return string.Concat(Spans.Select(s => s.Text));
        }
    }

    /// <summary>Builds a single-span line in the default ink.</summary>
    public static CodeLine FromPlain(string text) => new()
    {
        Spans = [new CodeSpan(text ?? string.Empty)],
    };
}
