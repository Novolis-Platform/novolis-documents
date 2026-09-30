namespace Novolis.Documents;

/// <summary>Fluent builder for <see cref="LastPage"/>.</summary>
public sealed class LastPageBuilder
{
    string? _title;
    readonly List<string> _lines = [];
    readonly DocumentContentBuilder _blocks = new();

    /// <summary>Closing title.</summary>
    public LastPageBuilder Title(string? title)
    {
        _title = title;
        return this;
    }

    /// <summary>Plain closing lines.</summary>
    public LastPageBuilder Lines(params string[] lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        _lines.AddRange(lines);
        return this;
    }

    /// <summary>Richer blocks after lines.</summary>
    public LastPageBuilder Blocks(Action<DocumentContentBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_blocks);
        return this;
    }

    /// <summary>Builds the last page.</summary>
    public LastPage Build() => new()
    {
        Title = _title,
        Lines = _lines.ToArray(),
        Blocks = _blocks.ToBlocks(),
    };
}
