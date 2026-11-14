namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Configures 1-Wire document creation and editor-session requests (appsettings section <c>OrganicPlugins:DocumentCreation</c>).
/// Limits and timeouts are application settings so no handler carries a fixed number.
/// </summary>
public sealed class OrganicDocumentCreationOptions
{
    /// <summary>Gets or sets the largest accepted document content, in characters.</summary>
    /// <value>Defaults to 400000, which keeps a create request well inside one 1-Wire message.</value>
    public int MaximumContentCharacters { get; set; } = 400000;

    /// <summary>Gets or sets the longest kept document title, in characters; longer titles are shortened.</summary>
    /// <value>Defaults to 120.</value>
    public int MaximumTitleCharacters { get; set; } = 120;

    /// <summary>Gets or sets how long one editor session may take to describe itself for the session list, in seconds.</summary>
    /// <value>Defaults to 10.</value>
    public int SessionDescriptionTimeoutSeconds { get; set; } = 10;

    /// <summary>Gets or sets how long the owning editor may take to create an approved document, in seconds.</summary>
    /// <value>Defaults to 30.</value>
    public int EditorCreationTimeoutSeconds { get; set; } = 30;
}
