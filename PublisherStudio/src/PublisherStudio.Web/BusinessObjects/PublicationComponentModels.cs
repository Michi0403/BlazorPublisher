using System.Text.Json.Serialization;

namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Browser-native DevExtreme controls that can be rendered in the editor, interactive
/// presentation export, and single-file website export without a Blazor or ASP.NET Core
/// runtime in the exported document.
/// </summary>
public enum PublicationComponentKind
{
    DataGrid,
    TreeList,
    Scheduler,
    Form,
    TextBox,
    TextArea,
    NumberBox,
    DateBox,
    CheckBox,
    SelectBox,
    TagBox,
    Gallery,
    TileView,
    Menu,
    ContextMenu,
    TabPanel,
    MultiView,
    Splitter,
    ScrollView,
    PivotGrid,
    Map,
    VectorMap,
    Chat,
    Button
}


/// <summary>
/// Lists supported publication chat platform values.
/// </summary>
public enum PublicationChatPlatform
{
    OutputContext,
    Preview,
    Twitch,
    YouTube,
    Custom
}

/// <summary>
/// Lists supported publication chat display mode values.
/// </summary>
public enum PublicationChatDisplayMode
{
    Auto,
    Interactive,
    ViewerOnly,
    StreamOverlay
}

/// <summary>
/// Lists supported publication vector map base layer values.
/// </summary>
public enum PublicationVectorMapBaseLayer
{
    World,
    Europe,
    Eurasia,
    Africa,
    Usa,
    Canada,
    None
}

/// <summary>
/// Lists supported publication vector feature kind values.
/// </summary>
public enum PublicationVectorFeatureKind
{
    Marker,
    Line,
    Polygon
}

/// <summary>
/// Represents a publication map point.
/// </summary>
public sealed class PublicationMapPoint
{
    /// <summary>
    /// Gets or sets longitude.
    /// </summary>
    public double Longitude { get; set; }
    /// <summary>
    /// Gets or sets latitude.
    /// </summary>
    public double Latitude { get; set; }
}

/// <summary>
/// Represents a publication vector map feature.
/// </summary>
public sealed class PublicationVectorMapFeature
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "Feature";
    /// <summary>
    /// Gets or sets kind.
    /// </summary>
    public PublicationVectorFeatureKind Kind { get; set; } = PublicationVectorFeatureKind.Marker;
    /// <summary>
    /// Gets or sets points.
    /// </summary>
    public List<PublicationMapPoint> Points { get; set; } = [];
    /// <summary>
    /// Gets or sets color.
    /// </summary>
    public string Color { get; set; } = "#2563eb";
    /// <summary>
    /// Gets or sets border color.
    /// </summary>
    public string BorderColor { get; set; } = "#1e3a8a";
    /// <summary>
    /// Gets or sets opacity.
    /// </summary>
    public double Opacity { get; set; } = .82;
    /// <summary>
    /// Gets or sets width.
    /// </summary>
    public double Width { get; set; } = 3;
    /// <summary>
    /// Gets or sets size.
    /// </summary>
    public double Size { get; set; } = 14;
    /// <summary>
    /// Gets or sets label.
    /// </summary>
    public string Label { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets value.
    /// </summary>
    public double? Value { get; set; }
}

/// <summary>
/// Lists supported publication component scope values.
/// </summary>
public enum PublicationComponentScope
{
    Page,
    Document
}

/// <summary>
/// Lists supported publication component data mode values.
/// </summary>
public enum PublicationComponentDataMode
{
    PublicationDataObject,
    StaticSnapshot,
    Rest,
    OData
}

/// <summary>
/// Lists supported publication component processing mode values.
/// </summary>
public enum PublicationComponentProcessingMode
{
    Client,
    Remote
}

/// <summary>
/// Lists supported publication component edit mode values.
/// </summary>
public enum PublicationComponentEditMode
{
    ReadOnly,
    Cell,
    Row,
    Batch,
    Form,
    Popup
}

/// <summary>
/// Lists supported publication component selection mode values.
/// </summary>
public enum PublicationComponentSelectionMode
{
    None,
    Single,
    Multiple
}

/// <summary>
/// Lists supported publication component editor kind values.
/// </summary>
public enum PublicationComponentEditorKind
{
    Auto,
    TextBox,
    TextArea,
    NumberBox,
    DateBox,
    CheckBox,
    SelectBox,
    TagBox
}

/// <summary>
/// Lists supported publication component field area values.
/// </summary>
public enum PublicationComponentFieldArea
{
    None,
    Row,
    Column,
    Data,
    Filter
}

/// <summary>
/// Lists supported publication component summary type values.
/// </summary>
public enum PublicationComponentSummaryType
{
    Sum,
    Count,
    Min,
    Max,
    Avg
}

/// <summary>
/// Lists supported publication component action trigger values.
/// </summary>
public enum PublicationComponentActionTrigger
{
    Click,
    ItemClick,
    SelectionChanged,
    ValueChanged,
    Submit,
    RowInserted,
    RowUpdated,
    RowRemoved,
    AppointmentAdded,
    AppointmentUpdated,
    AppointmentDeleted,
    MessageEntered
}

/// <summary>
/// Lists supported publication component action kind values.
/// </summary>
public enum PublicationComponentActionKind
{
    None,
    Navigate,
    NextPage,
    PreviousPage,
    GoToPage,
    OpenUrl,
    MailTo,
    Refresh,
    ShowElement,
    HideElement,
    ToggleElement,
    SubmitRest,
    SetValue,
    ApplyFilter,
    ClearFilter,
    CustomScript
}

/// <summary>
/// Lists supported publication component HTTP method values.
/// </summary>
public enum PublicationComponentHttpMethod
{
    Get,
    Post,
    Put,
    Patch,
    Delete
}

/// <summary>
/// Represents a publication component connection.
/// </summary>
public sealed class PublicationComponentConnection
{
    /// <summary>
    /// Gets or sets mode.
    /// </summary>
    public PublicationComponentDataMode Mode { get; set; } = PublicationComponentDataMode.PublicationDataObject;
    /// <summary>
    /// Gets or sets data object identifier.
    /// </summary>
    public Guid DataObjectId { get; set; }
    /// <summary>
    /// Gets or sets processing mode.
    /// </summary>
    public PublicationComponentProcessingMode ProcessingMode { get; set; } = PublicationComponentProcessingMode.Client;
    /// <summary>
    /// Gets or sets URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets load method.
    /// </summary>
    public PublicationComponentHttpMethod LoadMethod { get; set; } = PublicationComponentHttpMethod.Get;
    /// <summary>
    /// Gets or sets load body.
    /// </summary>
    public string LoadBody { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets JSON path.
    /// </summary>
    public string JsonPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets key field.
    /// </summary>
    public string KeyField { get; set; } = "id";
    /// <summary>
    /// Gets or sets key type.
    /// </summary>
    public string KeyType { get; set; } = "Int32";
    /// <summary>
    /// Gets or sets odata version.
    /// </summary>
    public int ODataVersion { get; set; } = 4;
    /// <summary>
    /// Gets or sets with credentials.
    /// </summary>
    public bool WithCredentials { get; set; }
    /// <summary>
    /// Gets or sets allow load.
    /// </summary>
    public bool AllowLoad { get; set; } = true;
    /// <summary>
    /// Gets or sets allow insert.
    /// </summary>
    public bool AllowInsert { get; set; }
    /// <summary>
    /// Gets or sets allow update.
    /// </summary>
    public bool AllowUpdate { get; set; }
    /// <summary>
    /// Gets or sets allow delete.
    /// </summary>
    public bool AllowDelete { get; set; }
    /// <summary>
    /// Gets or sets insert URL.
    /// </summary>
    public string InsertUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets update URL.
    /// </summary>
    public string UpdateUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets delete URL.
    /// </summary>
    public string DeleteUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets insert method.
    /// </summary>
    public PublicationComponentHttpMethod InsertMethod { get; set; } = PublicationComponentHttpMethod.Post;
    /// <summary>
    /// Gets or sets update method.
    /// </summary>
    public PublicationComponentHttpMethod UpdateMethod { get; set; } = PublicationComponentHttpMethod.Put;
    /// <summary>
    /// Gets or sets delete method.
    /// </summary>
    public PublicationComponentHttpMethod DeleteMethod { get; set; } = PublicationComponentHttpMethod.Delete;
    /// <summary>
    /// Gets or sets append key to write URL.
    /// </summary>
    public bool AppendKeyToWriteUrl { get; set; } = true;
    /// <summary>
    /// Gets or sets headers.
    /// </summary>
    public List<PublicationWebHeader> Headers { get; set; } = [];
}

/// <summary>
/// Represents a publication component field.
/// </summary>
public sealed class PublicationComponentField
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets data field.
    /// </summary>
    public string DataField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets caption.
    /// </summary>
    public string Caption { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets value kind.
    /// </summary>
    public PublicationDataValueKind ValueKind { get; set; } = PublicationDataValueKind.Text;
    /// <summary>
    /// Gets or sets editor.
    /// </summary>
    public PublicationComponentEditorKind Editor { get; set; } = PublicationComponentEditorKind.Auto;
    /// <summary>
    /// Gets or sets whether the item is visible.
    /// </summary>
    public bool Visible { get; set; } = true;
    /// <summary>
    /// Gets or sets editable.
    /// </summary>
    public bool Editable { get; set; } = true;
    /// <summary>
    /// Gets or sets required.
    /// </summary>
    public bool Required { get; set; }
    /// <summary>
    /// Gets or sets width.
    /// </summary>
    public int Width { get; set; }
    /// <summary>
    /// Gets or sets format.
    /// </summary>
    public string Format { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets area.
    /// </summary>
    public PublicationComponentFieldArea Area { get; set; }
    /// <summary>
    /// Gets or sets summary type.
    /// </summary>
    public PublicationComponentSummaryType SummaryType { get; set; } = PublicationComponentSummaryType.Sum;
    /// <summary>
    /// Gets or sets lookup data field.
    /// </summary>
    public string LookupDataField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets lookup display field.
    /// </summary>
    public string LookupDisplayField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets lookup data object identifier.
    /// </summary>
    public Guid? LookupDataObjectId { get; set; }
}

/// <summary>
/// Represents a publication component action.
/// </summary>
public sealed class PublicationComponentAction
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets trigger.
    /// </summary>
    public PublicationComponentActionTrigger Trigger { get; set; } = PublicationComponentActionTrigger.Click;
    /// <summary>
    /// Gets or sets action.
    /// </summary>
    public PublicationComponentActionKind Action { get; set; }
    /// <summary>
    /// Gets or sets target page identifier.
    /// </summary>
    public Guid? TargetPageId { get; set; }
    /// <summary>
    /// Gets or sets target element identifier.
    /// </summary>
    public Guid? TargetElementId { get; set; }
    /// <summary>
    /// Gets or sets target shared component identifier.
    /// </summary>
    public Guid? TargetSharedComponentId { get; set; }
    /// <summary>
    /// Gets or sets URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets open in new window.
    /// </summary>
    public bool OpenInNewWindow { get; set; } = true;
    /// <summary>
    /// Gets or sets mail to.
    /// </summary>
    public string MailTo { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets mail subject.
    /// </summary>
    public string MailSubject { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets mail body.
    /// </summary>
    public string MailBody { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets confirmation text.
    /// </summary>
    public string ConfirmationText { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets source field.
    /// </summary>
    public string SourceField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets target field.
    /// </summary>
    public string TargetField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets value template.
    /// </summary>
    public string ValueTemplate { get; set; } = "{{value}}";
    /// <summary>
    /// Gets or sets script.
    /// </summary>
    public string Script { get; set; } = string.Empty;
}


/// <summary>
/// Lists supported publication menu source mode values.
/// </summary>
public enum PublicationMenuSourceMode
{
    DataConnection,
    ManualItems
}

/// <summary>
/// Lists supported publication menu destination kind values.
/// </summary>
public enum PublicationMenuDestinationKind
{
    None,
    Page,
    ExternalUrl
}

/// <summary>
/// Represents a publication menu item.
/// </summary>
public sealed class PublicationMenuItem
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets parent identifier.
    /// </summary>
    public Guid? ParentId { get; set; }
    /// <summary>
    /// Gets or sets text.
    /// </summary>
    public string Text { get; set; } = "Menu item";
    /// <summary>
    /// Gets or sets destination.
    /// </summary>
    public PublicationMenuDestinationKind Destination { get; set; }
    /// <summary>
    /// Gets or sets target page identifier.
    /// </summary>
    public Guid? TargetPageId { get; set; }
    /// <summary>
    /// Gets or sets URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets open in new window.
    /// </summary>
    public bool OpenInNewWindow { get; set; } = true;
    /// <summary>
    /// Gets or sets whether the feature is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets whether the item is visible.
    /// </summary>
    public bool Visible { get; set; } = true;
    /// <summary>
    /// Gets or sets icon CSS class.
    /// </summary>
    public string IconCssClass { get; set; } = string.Empty;
}

/// <summary>
/// Represents a publication component panel.
/// </summary>
public sealed class PublicationComponentPanel
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets title.
    /// </summary>
    public string Title { get; set; } = "Panel";
    /// <summary>
    /// Gets or sets size.
    /// </summary>
    public string Size { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets min size.
    /// </summary>
    public string MinSize { get; set; } = "80px";
    /// <summary>
    /// Gets or sets max size.
    /// </summary>
    public string MaxSize { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets collapsible.
    /// </summary>
    public bool Collapsible { get; set; } = true;
    /// <summary>
    /// Gets or sets collapsed.
    /// </summary>
    public bool Collapsed { get; set; }
    /// <summary>
    /// Gets or sets child kind.
    /// </summary>
    public PublicationComponentKind ChildKind { get; set; } = PublicationComponentKind.DataGrid;
    /// <summary>
    /// Gets or sets data object identifier.
    /// </summary>
    public Guid DataObjectId { get; set; }
    /// <summary>
    /// Gets or sets content HTML.
    /// </summary>
    public string ContentHtml { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets fields.
    /// </summary>
    public List<PublicationComponentField> Fields { get; set; } = [];
}

/// <summary>
/// Represents a dev extreme component element.
/// </summary>
public sealed class DevExtremeComponentElement : PublicationElement
{
    /// <summary>
    /// Gets kind.
    /// </summary>
    public override PublicationElementKind Kind => PublicationElementKind.DevExtremeComponent;
    /// <summary>
    /// Gets or sets component kind.
    /// </summary>
    public PublicationComponentKind ComponentKind { get; set; } = PublicationComponentKind.DataGrid;
    /// <summary>
    /// Gets or sets scope.
    /// </summary>
    public PublicationComponentScope Scope { get; set; }
    /// <summary>
    /// Gets or sets shared component identifier.
    /// </summary>
    public Guid? SharedComponentId { get; set; }
    /// <summary>
    /// Gets or sets title.
    /// </summary>
    public string Title { get; set; } = "Data Grid";
    /// <summary>
    /// Gets or sets connection.
    /// </summary>
    public PublicationComponentConnection Connection { get; set; } = new();
    /// <summary>
    /// Gets or sets fields.
    /// </summary>
    public List<PublicationComponentField> Fields { get; set; } = [];
    /// <summary>
    /// Gets or sets actions.
    /// </summary>
    public List<PublicationComponentAction> Actions { get; set; } = [];
    /// <summary>
    /// Gets or sets panels.
    /// </summary>
    public List<PublicationComponentPanel> Panels { get; set; } = [];
    /// <summary>
    /// Gets or sets menu source mode.
    /// </summary>
    public PublicationMenuSourceMode MenuSourceMode { get; set; } = PublicationMenuSourceMode.DataConnection;
    /// <summary>
    /// Gets or sets menu items.
    /// </summary>
    public List<PublicationMenuItem> MenuItems { get; set; } = [];

    /// <summary>
    /// Gets or sets edit mode.
    /// </summary>
    public PublicationComponentEditMode EditMode { get; set; } = PublicationComponentEditMode.ReadOnly;
    /// <summary>
    /// Gets or sets selection mode.
    /// </summary>
    public PublicationComponentSelectionMode SelectionMode { get; set; } = PublicationComponentSelectionMode.Single;
    /// <summary>
    /// Gets or sets show title.
    /// </summary>
    public bool ShowTitle { get; set; } = true;
    /// <summary>
    /// Gets or sets show borders.
    /// </summary>
    public bool ShowBorders { get; set; } = true;
    /// <summary>
    /// Gets or sets show filter row.
    /// </summary>
    public bool ShowFilterRow { get; set; } = true;
    /// <summary>
    /// Gets or sets show header filter.
    /// </summary>
    public bool ShowHeaderFilter { get; set; }
    /// <summary>
    /// Gets or sets show search panel.
    /// </summary>
    public bool ShowSearchPanel { get; set; } = true;
    /// <summary>
    /// Gets or sets show group panel.
    /// </summary>
    public bool ShowGroupPanel { get; set; }
    /// <summary>
    /// Gets or sets show column chooser.
    /// </summary>
    public bool ShowColumnChooser { get; set; }
    /// <summary>
    /// Gets or sets allow sorting.
    /// </summary>
    public bool AllowSorting { get; set; } = true;
    /// <summary>
    /// Gets or sets allow filtering.
    /// </summary>
    public bool AllowFiltering { get; set; } = true;
    /// <summary>
    /// Gets or sets allow paging.
    /// </summary>
    public bool AllowPaging { get; set; } = true;
    /// <summary>
    /// Gets or sets allow reordering.
    /// </summary>
    public bool AllowReordering { get; set; } = true;
    /// <summary>
    /// Gets or sets allow resizing.
    /// </summary>
    public bool AllowResizing { get; set; } = true;
    /// <summary>
    /// Gets or sets word wrap.
    /// </summary>
    public bool WordWrap { get; set; }
    /// <summary>
    /// Gets or sets auto expand all.
    /// </summary>
    public bool AutoExpandAll { get; set; }
    /// <summary>
    /// Gets or sets page size.
    /// </summary>
    public int PageSize { get; set; } = 20;
    /// <summary>
    /// Gets or sets height mode.
    /// </summary>
    public string HeightMode { get; set; } = "fill";

    /// <summary>
    /// Gets or sets key field.
    /// </summary>
    public string KeyField { get; set; } = "id";
    /// <summary>
    /// Gets or sets parent field.
    /// </summary>
    public string ParentField { get; set; } = "parentId";
    /// <summary>
    /// Gets or sets text field.
    /// </summary>
    public string TextField { get; set; } = "text";
    /// <summary>
    /// Gets or sets value field.
    /// </summary>
    public string ValueField { get; set; } = "value";
    /// <summary>
    /// Gets or sets display field.
    /// </summary>
    public string DisplayField { get; set; } = "text";
    /// <summary>
    /// Gets or sets image field.
    /// </summary>
    public string ImageField { get; set; } = "image";
    /// <summary>
    /// Gets or sets media kind field.
    /// </summary>
    public string MediaKindField { get; set; } = "mediaType";
    /// <summary>
    /// Gets or sets media source field.
    /// </summary>
    public string MediaSourceField { get; set; } = "source";
    /// <summary>
    /// Gets or sets media poster field.
    /// </summary>
    public string MediaPosterField { get; set; } = "poster";
    /// <summary>
    /// Gets or sets media alt text field.
    /// </summary>
    public string MediaAltTextField { get; set; } = "altText";
    /// <summary>
    /// Gets or sets media show controls.
    /// </summary>
    public bool MediaShowControls { get; set; } = true;
    /// <summary>
    /// Gets or sets media auto play.
    /// </summary>
    public bool MediaAutoPlay { get; set; }
    /// <summary>
    /// Gets or sets media muted.
    /// </summary>
    public bool MediaMuted { get; set; } = true;
    /// <summary>
    /// Gets or sets media loop.
    /// </summary>
    public bool MediaLoop { get; set; } = true;
    /// <summary>
    /// Gets or sets start date field.
    /// </summary>
    public string StartDateField { get; set; } = "startDate";
    /// <summary>
    /// Gets or sets end date field.
    /// </summary>
    public string EndDateField { get; set; } = "endDate";
    /// <summary>
    /// Gets or sets all day field.
    /// </summary>
    public string AllDayField { get; set; } = "allDay";
    /// <summary>
    /// Gets or sets target page field.
    /// </summary>
    public string TargetPageField { get; set; } = "targetPageId";
    /// <summary>
    /// Gets or sets URL field.
    /// </summary>
    public string UrlField { get; set; } = "url";
    /// <summary>
    /// Gets or sets current view.
    /// </summary>
    public string CurrentView { get; set; } = "week";
    /// <summary>
    /// Gets or sets orientation.
    /// </summary>
    public string Orientation { get; set; } = "horizontal";
    /// <summary>
    /// Gets or sets column count.
    /// </summary>
    public int ColumnCount { get; set; } = 2;
    /// <summary>
    /// Gets or sets button text.
    /// </summary>
    public string ButtonText { get; set; } = "Run";
    /// <summary>
    /// Gets or sets chat platform.
    /// </summary>
    public PublicationChatPlatform ChatPlatform { get; set; } = PublicationChatPlatform.OutputContext;
    /// <summary>
    /// Gets or sets chat channel.
    /// </summary>
    public string ChatChannel { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets chat platform field.
    /// </summary>
    public string ChatPlatformField { get; set; } = "platform";
    /// <summary>
    /// Gets or sets chat channel field.
    /// </summary>
    public string ChatChannelField { get; set; } = "channel";
    /// <summary>
    /// Gets or sets chat message field.
    /// </summary>
    public string ChatMessageField { get; set; } = "text";
    /// <summary>
    /// Gets or sets chat timestamp field.
    /// </summary>
    public string ChatTimestampField { get; set; } = "timestamp";
    /// <summary>
    /// Gets or sets chat author identifier field.
    /// </summary>
    public string ChatAuthorIdField { get; set; } = "authorId";
    /// <summary>
    /// Gets or sets chat author name field.
    /// </summary>
    public string ChatAuthorNameField { get; set; } = "authorName";
    /// <summary>
    /// Gets or sets chat author avatar field.
    /// </summary>
    public string ChatAuthorAvatarField { get; set; } = "authorAvatar";
    /// <summary>
    /// Gets or sets chat current user identifier.
    /// </summary>
    public string ChatCurrentUserId { get; set; } = "publisher";
    /// <summary>
    /// Gets or sets chat current user name.
    /// </summary>
    public string ChatCurrentUserName { get; set; } = "Streamer";
    /// <summary>
    /// Gets or sets chat current user avatar.
    /// </summary>
    public string ChatCurrentUserAvatar { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets chat allow sending.
    /// </summary>
    public bool ChatAllowSending { get; set; } = true;
    /// <summary>
    /// Gets or sets chat show avatar.
    /// </summary>
    public bool ChatShowAvatar { get; set; } = true;
    /// <summary>
    /// Gets or sets chat show timestamp.
    /// </summary>
    public bool ChatShowTimestamp { get; set; } = true;
    /// <summary>
    /// Gets or sets chat optimistic send.
    /// </summary>
    public bool ChatOptimisticSend { get; set; } = true;
    /// <summary>
    /// Gets or sets chat display mode.
    /// </summary>
    public PublicationChatDisplayMode ChatDisplayMode { get; set; } = PublicationChatDisplayMode.Auto;
    /// <summary>
    /// Gets or sets chat max visible messages.
    /// </summary>
    public int ChatMaxVisibleMessages { get; set; } = 12;
    /// <summary>
    /// Gets or sets chat compact.
    /// </summary>
    public bool ChatCompact { get; set; }
    /// <summary>
    /// Gets or sets chat fade older messages.
    /// </summary>
    public bool ChatFadeOlderMessages { get; set; } = true;
    /// <summary>
    /// Gets or sets chat show platform badge.
    /// </summary>
    public bool ChatShowPlatformBadge { get; set; } = true;
    /// <summary>
    /// Gets or sets chat background opacity.
    /// </summary>
    public double ChatBackgroundOpacity { get; set; } = .88;
    /// <summary>
    /// Gets or sets chat message opacity.
    /// </summary>
    public double ChatMessageOpacity { get; set; } = .78;
    /// <summary>
    /// Gets or sets placeholder.
    /// </summary>
    public string Placeholder { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets initial value.
    /// </summary>
    public string InitialValue { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets background.
    /// </summary>
    public string Background { get; set; } = "#ffffff";
    /// <summary>
    /// Gets or sets border color.
    /// </summary>
    public string BorderColor { get; set; } = "#cbd5e1";
    /// <summary>
    /// Gets or sets border width millimetres.
    /// </summary>
    public double BorderWidthMm { get; set; } = .25;

    /// <summary>
    /// Gets or sets custom CSS class.
    /// </summary>
    public string CustomCssClass { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets custom CSS.
    /// </summary>
    public string CustomCss { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets content offset horizontal position.
    /// </summary>
    public double ContentOffsetX { get; set; }
    /// <summary>
    /// Gets or sets content offset vertical position.
    /// </summary>
    public double ContentOffsetY { get; set; }
    /// <summary>
    /// Gets or sets content scale.
    /// </summary>
    public double ContentScale { get; set; } = 1;

    /// <summary>
    /// Gets or sets map provider.
    /// </summary>
    public string MapProvider { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets map type.
    /// </summary>
    public string MapType { get; set; } = "roadmap";
    /// <summary>
    /// Gets or sets map API key.
    /// </summary>
    public string MapApiKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets map identifier.
    /// </summary>
    public string MapId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets map center latitude.
    /// </summary>
    public double MapCenterLatitude { get; set; } = 51.1657;
    /// <summary>
    /// Gets or sets map center longitude.
    /// </summary>
    public double MapCenterLongitude { get; set; } = 10.4515;
    /// <summary>
    /// Gets or sets map zoom.
    /// </summary>
    public double MapZoom { get; set; } = 4;
    /// <summary>
    /// Gets or sets map controls.
    /// </summary>
    public bool MapControls { get; set; } = true;
    /// <summary>
    /// Gets or sets map auto adjust.
    /// </summary>
    public bool MapAutoAdjust { get; set; } = true;
    /// <summary>
    /// Gets or sets map show routes.
    /// </summary>
    public bool MapShowRoutes { get; set; } = true;
    /// <summary>
    /// Gets or sets latitude field.
    /// </summary>
    public string LatitudeField { get; set; } = "latitude";
    /// <summary>
    /// Gets or sets longitude field.
    /// </summary>
    public string LongitudeField { get; set; } = "longitude";
    /// <summary>
    /// Gets or sets address field.
    /// </summary>
    public string AddressField { get; set; } = "address";
    /// <summary>
    /// Gets or sets marker tooltip field.
    /// </summary>
    public string MarkerTooltipField { get; set; } = "text";
    /// <summary>
    /// Gets or sets map route field.
    /// </summary>
    public string MapRouteField { get; set; } = "routeId";
    /// <summary>
    /// Gets or sets map order field.
    /// </summary>
    public string MapOrderField { get; set; } = "order";

    /// <summary>
    /// Gets or sets vector base layer.
    /// </summary>
    public PublicationVectorMapBaseLayer VectorBaseLayer { get; set; } = PublicationVectorMapBaseLayer.World;
    /// <summary>
    /// Gets or sets vector projection.
    /// </summary>
    public string VectorProjection { get; set; } = "mercator";
    /// <summary>
    /// Gets or sets vector show labels.
    /// </summary>
    public bool VectorShowLabels { get; set; } = true;
    /// <summary>
    /// Gets or sets vector label field.
    /// </summary>
    public string VectorLabelField { get; set; } = "name";
    /// <summary>
    /// Gets or sets vector value field.
    /// </summary>
    public string VectorValueField { get; set; } = "value";
    /// <summary>
    /// Gets or sets vector color field.
    /// </summary>
    public string VectorColorField { get; set; } = "color";
    /// <summary>
    /// Gets or sets vector features.
    /// </summary>
    public List<PublicationVectorMapFeature> VectorFeatures { get; set; } = [];

    /// <summary>Additional DevExtreme options merged after the safe generated options.</summary>
    public string AdvancedOptionsJson { get; set; } = "{}";
    /// <summary>
    /// Gets or sets allow custom script.
    /// </summary>
    public bool AllowCustomScript { get; set; }

    /// <summary>
    /// Gets is layout container.
    /// </summary>
    [JsonIgnore]
    public bool IsLayoutContainer => ComponentKind is PublicationComponentKind.Splitter
        or PublicationComponentKind.TabPanel
        or PublicationComponentKind.MultiView
        or PublicationComponentKind.ScrollView;

    /// <summary>
    /// Gets supports content viewport.
    /// </summary>
    [JsonIgnore]
    public bool SupportsContentViewport => ComponentKind is PublicationComponentKind.Map or PublicationComponentKind.VectorMap;
}
