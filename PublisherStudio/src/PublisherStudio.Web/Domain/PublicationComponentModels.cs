using System.Text.Json.Serialization;

namespace PublisherStudio.Domain;

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


public enum PublicationChatPlatform
{
    OutputContext,
    Preview,
    Twitch,
    YouTube,
    Custom
}

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

public enum PublicationVectorFeatureKind
{
    Marker,
    Line,
    Polygon
}

public sealed class PublicationMapPoint
{
    public double Longitude { get; set; }
    public double Latitude { get; set; }
}

public sealed class PublicationVectorMapFeature
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Feature";
    public PublicationVectorFeatureKind Kind { get; set; } = PublicationVectorFeatureKind.Marker;
    public List<PublicationMapPoint> Points { get; set; } = [];
    public string Color { get; set; } = "#2563eb";
    public string BorderColor { get; set; } = "#1e3a8a";
    public double Opacity { get; set; } = .82;
    public double Width { get; set; } = 3;
    public double Size { get; set; } = 14;
    public string Label { get; set; } = string.Empty;
    public double? Value { get; set; }
}

public enum PublicationComponentScope
{
    Page,
    Document
}

public enum PublicationComponentDataMode
{
    PublicationDataObject,
    StaticSnapshot,
    Rest,
    OData
}

public enum PublicationComponentProcessingMode
{
    Client,
    Remote
}

public enum PublicationComponentEditMode
{
    ReadOnly,
    Cell,
    Row,
    Batch,
    Form,
    Popup
}

public enum PublicationComponentSelectionMode
{
    None,
    Single,
    Multiple
}

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

public enum PublicationComponentFieldArea
{
    None,
    Row,
    Column,
    Data,
    Filter
}

public enum PublicationComponentSummaryType
{
    Sum,
    Count,
    Min,
    Max,
    Avg
}

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

public enum PublicationComponentHttpMethod
{
    Get,
    Post,
    Put,
    Patch,
    Delete
}

public sealed class PublicationComponentConnection
{
    public PublicationComponentDataMode Mode { get; set; } = PublicationComponentDataMode.PublicationDataObject;
    public Guid DataObjectId { get; set; }
    public PublicationComponentProcessingMode ProcessingMode { get; set; } = PublicationComponentProcessingMode.Client;
    public string Url { get; set; } = string.Empty;
    public PublicationComponentHttpMethod LoadMethod { get; set; } = PublicationComponentHttpMethod.Get;
    public string LoadBody { get; set; } = string.Empty;
    public string JsonPath { get; set; } = string.Empty;
    public string KeyField { get; set; } = "id";
    public string KeyType { get; set; } = "Int32";
    public int ODataVersion { get; set; } = 4;
    public bool WithCredentials { get; set; }
    public bool AllowLoad { get; set; } = true;
    public bool AllowInsert { get; set; }
    public bool AllowUpdate { get; set; }
    public bool AllowDelete { get; set; }
    public string InsertUrl { get; set; } = string.Empty;
    public string UpdateUrl { get; set; } = string.Empty;
    public string DeleteUrl { get; set; } = string.Empty;
    public PublicationComponentHttpMethod InsertMethod { get; set; } = PublicationComponentHttpMethod.Post;
    public PublicationComponentHttpMethod UpdateMethod { get; set; } = PublicationComponentHttpMethod.Put;
    public PublicationComponentHttpMethod DeleteMethod { get; set; } = PublicationComponentHttpMethod.Delete;
    public bool AppendKeyToWriteUrl { get; set; } = true;
    public List<PublicationWebHeader> Headers { get; set; } = [];
}

public sealed class PublicationComponentField
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DataField { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public PublicationDataValueKind ValueKind { get; set; } = PublicationDataValueKind.Text;
    public PublicationComponentEditorKind Editor { get; set; } = PublicationComponentEditorKind.Auto;
    public bool Visible { get; set; } = true;
    public bool Editable { get; set; } = true;
    public bool Required { get; set; }
    public int Width { get; set; }
    public string Format { get; set; } = string.Empty;
    public PublicationComponentFieldArea Area { get; set; }
    public PublicationComponentSummaryType SummaryType { get; set; } = PublicationComponentSummaryType.Sum;
    public string LookupDataField { get; set; } = string.Empty;
    public string LookupDisplayField { get; set; } = string.Empty;
    public Guid? LookupDataObjectId { get; set; }
}

public sealed class PublicationComponentAction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public PublicationComponentActionTrigger Trigger { get; set; } = PublicationComponentActionTrigger.Click;
    public PublicationComponentActionKind Action { get; set; }
    public Guid? TargetPageId { get; set; }
    public Guid? TargetElementId { get; set; }
    public Guid? TargetSharedComponentId { get; set; }
    public string Url { get; set; } = string.Empty;
    public bool OpenInNewWindow { get; set; } = true;
    public string MailTo { get; set; } = string.Empty;
    public string MailSubject { get; set; } = string.Empty;
    public string MailBody { get; set; } = string.Empty;
    public string ConfirmationText { get; set; } = string.Empty;
    public string SourceField { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
    public string ValueTemplate { get; set; } = "{{value}}";
    public string Script { get; set; } = string.Empty;
}


public enum PublicationMenuSourceMode
{
    DataConnection,
    ManualItems
}

public enum PublicationMenuDestinationKind
{
    None,
    Page,
    ExternalUrl
}

public sealed class PublicationMenuItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ParentId { get; set; }
    public string Text { get; set; } = "Menu item";
    public PublicationMenuDestinationKind Destination { get; set; }
    public Guid? TargetPageId { get; set; }
    public string Url { get; set; } = string.Empty;
    public bool OpenInNewWindow { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public bool Visible { get; set; } = true;
    public string IconCssClass { get; set; } = string.Empty;
}

public sealed class PublicationComponentPanel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "Panel";
    public string Size { get; set; } = string.Empty;
    public string MinSize { get; set; } = "80px";
    public string MaxSize { get; set; } = string.Empty;
    public bool Collapsible { get; set; } = true;
    public bool Collapsed { get; set; }
    public PublicationComponentKind ChildKind { get; set; } = PublicationComponentKind.DataGrid;
    public Guid DataObjectId { get; set; }
    public string ContentHtml { get; set; } = string.Empty;
    public List<PublicationComponentField> Fields { get; set; } = [];
}

public sealed class DevExtremeComponentElement : PublicationElement
{
    public override PublicationElementKind Kind => PublicationElementKind.DevExtremeComponent;
    public PublicationComponentKind ComponentKind { get; set; } = PublicationComponentKind.DataGrid;
    public PublicationComponentScope Scope { get; set; }
    public Guid? SharedComponentId { get; set; }
    public string Title { get; set; } = "Data Grid";
    public PublicationComponentConnection Connection { get; set; } = new();
    public List<PublicationComponentField> Fields { get; set; } = [];
    public List<PublicationComponentAction> Actions { get; set; } = [];
    public List<PublicationComponentPanel> Panels { get; set; } = [];
    public PublicationMenuSourceMode MenuSourceMode { get; set; } = PublicationMenuSourceMode.DataConnection;
    public List<PublicationMenuItem> MenuItems { get; set; } = [];

    public PublicationComponentEditMode EditMode { get; set; } = PublicationComponentEditMode.ReadOnly;
    public PublicationComponentSelectionMode SelectionMode { get; set; } = PublicationComponentSelectionMode.Single;
    public bool ShowTitle { get; set; } = true;
    public bool ShowBorders { get; set; } = true;
    public bool ShowFilterRow { get; set; } = true;
    public bool ShowHeaderFilter { get; set; }
    public bool ShowSearchPanel { get; set; } = true;
    public bool ShowGroupPanel { get; set; }
    public bool ShowColumnChooser { get; set; }
    public bool AllowSorting { get; set; } = true;
    public bool AllowFiltering { get; set; } = true;
    public bool AllowPaging { get; set; } = true;
    public bool AllowReordering { get; set; } = true;
    public bool AllowResizing { get; set; } = true;
    public bool WordWrap { get; set; }
    public bool AutoExpandAll { get; set; }
    public int PageSize { get; set; } = 20;
    public string HeightMode { get; set; } = "fill";

    public string KeyField { get; set; } = "id";
    public string ParentField { get; set; } = "parentId";
    public string TextField { get; set; } = "text";
    public string ValueField { get; set; } = "value";
    public string DisplayField { get; set; } = "text";
    public string ImageField { get; set; } = "image";
    public string MediaKindField { get; set; } = "mediaType";
    public string MediaSourceField { get; set; } = "source";
    public string MediaPosterField { get; set; } = "poster";
    public string MediaAltTextField { get; set; } = "altText";
    public bool MediaShowControls { get; set; } = true;
    public bool MediaAutoPlay { get; set; }
    public bool MediaMuted { get; set; } = true;
    public bool MediaLoop { get; set; } = true;
    public string StartDateField { get; set; } = "startDate";
    public string EndDateField { get; set; } = "endDate";
    public string AllDayField { get; set; } = "allDay";
    public string TargetPageField { get; set; } = "targetPageId";
    public string UrlField { get; set; } = "url";
    public string CurrentView { get; set; } = "week";
    public string Orientation { get; set; } = "horizontal";
    public int ColumnCount { get; set; } = 2;
    public string ButtonText { get; set; } = "Run";
    public PublicationChatPlatform ChatPlatform { get; set; } = PublicationChatPlatform.OutputContext;
    public string ChatChannel { get; set; } = string.Empty;
    public string ChatPlatformField { get; set; } = "platform";
    public string ChatChannelField { get; set; } = "channel";
    public string ChatMessageField { get; set; } = "text";
    public string ChatTimestampField { get; set; } = "timestamp";
    public string ChatAuthorIdField { get; set; } = "authorId";
    public string ChatAuthorNameField { get; set; } = "authorName";
    public string ChatAuthorAvatarField { get; set; } = "authorAvatar";
    public string ChatCurrentUserId { get; set; } = "publisher";
    public string ChatCurrentUserName { get; set; } = "Streamer";
    public string ChatCurrentUserAvatar { get; set; } = string.Empty;
    public bool ChatAllowSending { get; set; } = true;
    public bool ChatShowAvatar { get; set; } = true;
    public bool ChatShowTimestamp { get; set; } = true;
    public bool ChatOptimisticSend { get; set; } = true;
    public string Placeholder { get; set; } = string.Empty;
    public string InitialValue { get; set; } = string.Empty;
    public string Background { get; set; } = "#ffffff";
    public string BorderColor { get; set; } = "#cbd5e1";
    public double BorderWidthMm { get; set; } = .25;

    public string CustomCssClass { get; set; } = string.Empty;
    public string CustomCss { get; set; } = string.Empty;
    public double ContentOffsetX { get; set; }
    public double ContentOffsetY { get; set; }
    public double ContentScale { get; set; } = 1;

    public string MapProvider { get; set; } = "google";
    public string MapType { get; set; } = "roadmap";
    public string MapApiKey { get; set; } = string.Empty;
    public double MapCenterLatitude { get; set; } = 51.1657;
    public double MapCenterLongitude { get; set; } = 10.4515;
    public double MapZoom { get; set; } = 4;
    public bool MapControls { get; set; } = true;
    public bool MapAutoAdjust { get; set; } = true;
    public bool MapShowRoutes { get; set; } = true;
    public string LatitudeField { get; set; } = "latitude";
    public string LongitudeField { get; set; } = "longitude";
    public string AddressField { get; set; } = "address";
    public string MarkerTooltipField { get; set; } = "text";
    public string MapRouteField { get; set; } = "routeId";
    public string MapOrderField { get; set; } = "order";

    public PublicationVectorMapBaseLayer VectorBaseLayer { get; set; } = PublicationVectorMapBaseLayer.World;
    public string VectorProjection { get; set; } = "mercator";
    public bool VectorShowLabels { get; set; } = true;
    public string VectorLabelField { get; set; } = "name";
    public string VectorValueField { get; set; } = "value";
    public string VectorColorField { get; set; } = "color";
    public List<PublicationVectorMapFeature> VectorFeatures { get; set; } = [];

    /// <summary>Additional DevExtreme options merged after the safe generated options.</summary>
    public string AdvancedOptionsJson { get; set; } = "{}";
    public bool AllowCustomScript { get; set; }

    [JsonIgnore]
    public bool IsLayoutContainer => ComponentKind is PublicationComponentKind.Splitter
        or PublicationComponentKind.TabPanel
        or PublicationComponentKind.MultiView
        or PublicationComponentKind.ScrollView;

    [JsonIgnore]
    public bool SupportsContentViewport => ComponentKind is PublicationComponentKind.Map or PublicationComponentKind.VectorMap;
}
