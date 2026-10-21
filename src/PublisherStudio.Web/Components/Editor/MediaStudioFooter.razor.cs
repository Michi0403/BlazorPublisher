using Microsoft.AspNetCore.Components;

namespace PublisherStudio.Components.Editor;

/// <summary>Renders the Media Studio transport/footer commands without assuming ownership of saving, recording, or exporting.</summary>
public partial class MediaStudioFooter
{
    /// <summary>Gets or sets the status calculated by the owning studio.</summary>
    /// <value>Localized footer status.</value>
    [Parameter] public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the owning studio currently supports downloading its media.</summary>
    /// <value>True when the download button is visible.</value>
    [Parameter] public bool DownloadAvailable { get; set; }

    /// <summary>Gets or sets the localized media download label.</summary>
    /// <value>Button caption for downloading the current media.</value>
    [Parameter] public string DownloadLabel { get; set; } = string.Empty;

    /// <summary>Gets or sets the localized apply/save caption.</summary>
    /// <value>Caption displayed by the primary action.</value>
    [Parameter] public string ApplyLabel { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the owning studio allows applying the current selection.</summary>
    /// <value>True when the primary action is enabled.</value>
    [Parameter] public bool ApplyEnabled { get; set; }

    /// <summary>Gets or sets the localized cancellation caption.</summary>
    /// <value>Label displayed by the cancel button.</value>
    [Parameter] public string CancelLabel { get; set; } = string.Empty;

    /// <summary>Gets or sets the existing download callback owned by the studio.</summary>
    /// <value>Callback executed to download the current media.</value>
    [Parameter] public EventCallback DownloadRequested { get; set; }

    /// <summary>Gets or sets the existing cancel callback owned by the studio.</summary>
    /// <value>Callback executed to close without saving.</value>
    [Parameter] public EventCallback CancelRequested { get; set; }

    /// <summary>Gets or sets the existing save callback owned by the studio.</summary>
    /// <value>Callback executed to save/apply the current media.</value>
    [Parameter] public EventCallback ApplyRequested { get; set; }
}
