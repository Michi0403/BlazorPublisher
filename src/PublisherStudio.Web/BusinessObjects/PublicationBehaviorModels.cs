namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Defines browser/runtime events that can start a declarative publication behavior.
/// </summary>
public enum PublicationBehaviorTrigger
{
    /// <summary>Runs the behavior when the authored object is clicked.</summary>
    Click,
    /// <summary>Runs the behavior when the authored object is double-clicked.</summary>
    DoubleClick,
    /// <summary>Runs the behavior when the authored object reports a value change.</summary>
    Change,
    /// <summary>Runs the behavior when the authored object receives focus.</summary>
    Focus,
    /// <summary>Runs the behavior when the authored object loses focus.</summary>
    Blur,
    /// <summary>Runs the behavior when a pointer enters the authored object.</summary>
    PointerEnter,
    /// <summary>Runs the behavior when a pointer leaves the authored object.</summary>
    PointerLeave,
    /// <summary>Runs the behavior once after the authored object runtime is attached.</summary>
    Load
}

/// <summary>
/// Defines safe declarative actions exposed by the publication object interface.
/// </summary>
public enum PublicationBehaviorAction
{
    /// <summary>Does not perform an action.</summary>
    None,
    /// <summary>Invokes the target object's ordinary click behavior.</summary>
    Click,
    /// <summary>Moves browser focus to the target object.</summary>
    Focus,
    /// <summary>Removes browser focus from the target object.</summary>
    Blur,
    /// <summary>Asks a data-aware target to refresh its current data.</summary>
    RefreshData,
    /// <summary>Shows the target object.</summary>
    Show,
    /// <summary>Hides the target object.</summary>
    Hide,
    /// <summary>Toggles target visibility.</summary>
    ToggleVisibility,
    /// <summary>Enables the target object where the runtime exposes an enabled/disabled option.</summary>
    Enable,
    /// <summary>Disables the target object where the runtime exposes an enabled/disabled option.</summary>
    Disable,
    /// <summary>
    /// Selects the set text option for <see cref="PublicationBehaviorAction"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    SetText,
    /// <summary>
    /// Selects the set value option for <see cref="PublicationBehaviorAction"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    SetValue,
    /// <summary>Invokes one allow-listed common component method.</summary>
    CallMethod,
    /// <summary>Navigates to a publication page.</summary>
    GoToPage,
    /// <summary>Navigates to the next publication page.</summary>
    NextPage,
    /// <summary>Navigates to the previous publication page.</summary>
    PreviousPage,
    /// <summary>
    /// Selects the open URL option for <see cref="PublicationBehaviorAction"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    OpenUrl
}

/// <summary>
/// Stores one declarative event-to-action rule attached to a publication object.
/// The rule is persisted with the publication and is executed by the exported browser runtime.
/// </summary>
public sealed class PublicationBehavior
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this publication behavior instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="PublicationBehavior"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets a value indicating whether the option is enabled applies to the publication behavior state.
    /// </summary>
    /// <value>The enabled value exposed by <see cref="PublicationBehavior"/>.</value>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets the trigger value that forms part of the publication behavior state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The trigger value exposed by <see cref="PublicationBehavior"/>.</value>
    public PublicationBehaviorTrigger Trigger { get; set; } = PublicationBehaviorTrigger.Click;
    /// <summary>
    /// Gets or sets the action value that forms part of the publication behavior state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The action value exposed by <see cref="PublicationBehavior"/>.</value>
    public PublicationBehaviorAction Action { get; set; } = PublicationBehaviorAction.Click;
    /// <summary>Gets or sets the target element identifier; null means the source object itself.</summary>
    /// <value>The target element identifier value exposed by <see cref="PublicationBehavior"/>.</value>
    public Guid? TargetElementId { get; set; }
    /// <summary>Gets or sets the target page identifier for page-navigation behavior.</summary>
    /// <value>The target page identifier value exposed by <see cref="PublicationBehavior"/>.</value>
    public Guid? TargetPageId { get; set; }
    /// <summary>
    /// Gets or sets the method value that forms part of the publication behavior state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The method value exposed by <see cref="PublicationBehavior"/>.</value>
    public string Method { get; set; } = string.Empty;
    /// <summary>Gets or sets the simple string value used by set-text or set-value behavior.</summary>
    /// <value>The value value exposed by <see cref="PublicationBehavior"/>.</value>
    public string Value { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the URL that identifies the network or application endpoint associated with this publication behavior state.
    /// </summary>
    /// <value>The URL value exposed by <see cref="PublicationBehavior"/>.</value>
    public string Url { get; set; } = string.Empty;
    /// <summary>Gets or sets whether an external address opens in a new browser window.</summary>
    /// <value>The open in new window value exposed by <see cref="PublicationBehavior"/>.</value>
    public bool OpenInNewWindow { get; set; } = true;
}

/// <summary>
/// Describes a publication object that can be selected as a behavior or script target in Panel Studio.
/// </summary>
/// <param name="ElementId">Stable publication element identifier.</param>
/// <param name="Name">Human-readable authored object name.</param>
/// <param name="Kind">Publication element kind.</param>
/// <param name="Address">Stable publication object address available to the browser runtime.</param>
/// <param name="Scope">Human-readable scope such as current panel or publication page.</param>
public sealed record PublicationObjectAddressOption(Guid ElementId, string Name, string Kind, string Address, string Scope = "Panel");

/// <summary>
/// Describes a script helper snippet that can be inserted by the publication editor without requiring the user to memorize runtime APIs.
/// </summary>
/// <param name="Key">Stable helper identifier.</param>
/// <param name="Label">Short editor label.</param>
/// <param name="Description">Plain-language explanation of the generated script.</param>
/// <param name="Code">JavaScript snippet inserted into an explicitly enabled custom-script action.</param>
public sealed record PublicationScriptHelperOption(string Key, string Label, string Description, string Code);
