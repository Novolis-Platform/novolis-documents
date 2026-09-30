namespace Novolis.Documents;

/// <summary>One colored run inside a code line (syntax highlighting).</summary>
/// <param name="Text">Literal text (may be empty).</param>
/// <param name="Color">Ink; null uses the parent <see cref="CodeBlock.TextColor"/>.</param>
public readonly record struct CodeSpan(string Text, DocumentColor? Color = null);
