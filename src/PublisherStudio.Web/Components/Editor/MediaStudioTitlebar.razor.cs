using Microsoft.AspNetCore.Components;

namespace PublisherStudio.Components.Editor;

/// <summary>Displays the localized Media Studio popup heading and forwards its close action to the owning studio.</summary>
public partial class MediaStudioTitlebar
{
    /// <summary>Gets or sets the already-localized title.</summary>
    /// <value>Title rendered in the studio header.</value>
    [Parameter] public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the already-localized explanation of the current editing task.</summary>
    /// <value>Subtitle rendered in the studio header.</value>
    [Parameter] public string Subtitle { get; set; } = string.Empty;

    /// <summary>Gets or sets the localized close button tooltip.</summary>
    /// <value>Tooltip displayed by the close button.</value>
    [Parameter] public string CloseLabel { get; set; } = string.Empty;

    /// <summary>Gets or sets the existing Media Studio close callback.</summary>
    /// <value>Callback to the parent that retains responsibility for cancellation.</value>
    [Parameter] public EventCallback CloseRequested { get; set; }
}
