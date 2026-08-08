namespace Novolis.Documents;

/// <summary>Entry point for the fluent document construction DSL.</summary>
public static class Document
{
    /// <summary>Starts a new document builder.</summary>
    public static DocumentBuilder Create() => new();

    /// <summary>Starts a builder with a title.</summary>
    public static DocumentBuilder Create(string title) => new DocumentBuilder().Title(title);
}
