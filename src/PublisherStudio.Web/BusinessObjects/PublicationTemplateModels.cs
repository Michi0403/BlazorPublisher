namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Describes one full-publication template discovered from PublisherStudio's local template library.
/// </summary>
/// <param name="Id">Stable file-relative identifier used to open the template without exposing an arbitrary filesystem path.</param>
/// <param name="Name">Display name read from the publication document.</param>
/// <param name="Description">Short capability summary shown in the New-from-template chooser.</param>
/// <param name="Category">Template category shown by the chooser.</param>
/// <param name="PageCount">Number of pages contained by the template.</param>
/// <param name="FileName">Template file name inside the PublisherTemplates directory.</param>
/// <param name="ModifiedUtc">Last-write timestamp of the template file.</param>
public sealed record PublicationTemplateDescriptor(
    string Id,
    string Name,
    string Description,
    string Category,
    int PageCount,
    string FileName,
    DateTimeOffset ModifiedUtc);

/// <summary>
/// Describes one reusable Panel/Div template discovered from PublisherStudio's local DivTemplates library.
/// </summary>
/// <param name="Id">Stable file-relative identifier used to open the template without exposing an arbitrary filesystem path.</param>
/// <param name="Name">Display name shown by Panel Library and Panel Studio.</param>
/// <param name="Description">Short description of the reusable panel composition.</param>
/// <param name="Category">Palette category used to group the template.</param>
/// <param name="IconCssClass">PublisherStudio icon class used by the template card and palette tool.</param>
/// <param name="PreviewKind">Preview kind used by Panel Studio's existing component-tool presentation.</param>
/// <param name="FileName">Template file name inside the DivTemplates directory.</param>
/// <param name="ModifiedUtc">Last-write timestamp of the template file.</param>
public sealed record DivTemplateDescriptor(
    string Id,
    string Name,
    string Description,
    string Category,
    string IconCssClass,
    string PreviewKind,
    string FileName,
    DateTimeOffset ModifiedUtc);
