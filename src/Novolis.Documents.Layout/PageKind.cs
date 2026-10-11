using Novolis.Documents;

namespace Novolis.Documents.Layout;

/// <summary>Kind of finished page.</summary>
public enum PageKind
{
    /// <summary>Dedicated opening page.</summary>
    First,

    /// <summary>Compatibility alias for the former opening-page name.</summary>
    Cover = First,

    /// <summary>Normal flowing page.</summary>
    Body = 1,

    /// <summary>Dedicated closing page.</summary>
    Last = 2,

    /// <summary>Legacy contents-page role.</summary>
    Toc = 3,
}
