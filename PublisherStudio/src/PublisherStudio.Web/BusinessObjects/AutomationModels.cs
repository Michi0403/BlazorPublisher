namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Lists supported browser automation command kind values.
/// </summary>
public enum BrowserAutomationCommandKind
{
    Click, DoubleClick, ContextMenu, MouseMove, MouseDown, MouseUp, Wheel, Focus, Blur, TypeText, SetValue, KeyDown, KeyUp, KeyPress
}

/// <summary>
/// Lists supported automation request status values.
/// </summary>
public enum AutomationRequestStatus { Pending, Claimed, Completed, Failed, Cancelled }

/// <summary>
/// Represents a browser automation command.
/// </summary>
public sealed class BrowserAutomationCommand
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets kind.
    /// </summary>
    public BrowserAutomationCommandKind Kind { get; set; }
    /// <summary>
    /// Gets or sets selector.
    /// </summary>
    public string Selector { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets text.
    /// </summary>
    public string Text { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets key.
    /// </summary>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets code.
    /// </summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets button.
    /// </summary>
    public int Button { get; set; }
    /// <summary>
    /// Gets or sets horizontal position.
    /// </summary>
    public double X { get; set; }
    /// <summary>
    /// Gets or sets vertical position.
    /// </summary>
    public double Y { get; set; }
    /// <summary>
    /// Gets or sets delta horizontal position.
    /// </summary>
    public double DeltaX { get; set; }
    /// <summary>
    /// Gets or sets delta vertical position.
    /// </summary>
    public double DeltaY { get; set; }
    /// <summary>
    /// Gets or sets ctrl key.
    /// </summary>
    public bool CtrlKey { get; set; }
    /// <summary>
    /// Gets or sets shift key.
    /// </summary>
    public bool ShiftKey { get; set; }
    /// <summary>
    /// Gets or sets alt key.
    /// </summary>
    public bool AltKey { get; set; }
    /// <summary>
    /// Gets or sets meta key.
    /// </summary>
    public bool MetaKey { get; set; }
    /// <summary>
    /// Gets or sets the UTC creation time.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets completed UTC.
    /// </summary>
    public DateTimeOffset? CompletedUtc { get; set; }
    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public AutomationRequestStatus Status { get; set; } = AutomationRequestStatus.Pending;
    /// <summary>
    /// Gets or sets result.
    /// </summary>
    public string Result { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets error.
    /// </summary>
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// Represents a browser screenshot request.
/// </summary>
public sealed class BrowserScreenshotRequest
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets selector.
    /// </summary>
    public string Selector { get; set; } = "body";
    /// <summary>
    /// Gets or sets format.
    /// </summary>
    public string Format { get; set; } = "png";
    /// <summary>
    /// Gets or sets quality.
    /// </summary>
    public double Quality { get; set; } = .92;
    /// <summary>
    /// Gets or sets scale.
    /// </summary>
    public double Scale { get; set; } = 1;
    /// <summary>
    /// Gets or sets include metadata.
    /// </summary>
    public bool IncludeMetadata { get; set; } = true;
    /// <summary>
    /// Gets or sets the UTC creation time.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets completed UTC.
    /// </summary>
    public DateTimeOffset? CompletedUtc { get; set; }
    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public AutomationRequestStatus Status { get; set; } = AutomationRequestStatus.Pending;
    /// <summary>
    /// Gets or sets data URL.
    /// </summary>
    public string DataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets pixel width.
    /// </summary>
    public int PixelWidth { get; set; }
    /// <summary>
    /// Gets or sets pixel height.
    /// </summary>
    public int PixelHeight { get; set; }
    /// <summary>
    /// Gets or sets error.
    /// </summary>
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// Represents an automation completion.
/// </summary>
public sealed record AutomationCompletion(string Result = "", string Error = "");
/// <summary>
/// Represents a screenshot completion.
/// </summary>
public sealed record ScreenshotCompletion(string DataUrl, int PixelWidth, int PixelHeight, string Error = "");

/// <summary>
/// Represents a business object descriptor.
/// </summary>
public sealed class BusinessObjectDescriptor
{
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets full name.
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets kind.
    /// </summary>
    public string Kind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets properties.
    /// </summary>
    public List<string> Properties { get; set; } = [];
}

/// <summary>
/// Represents a service API descriptor.
/// </summary>
public sealed class ServiceApiDescriptor
{
    /// <summary>
    /// Gets or sets service.
    /// </summary>
    public string Service { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets interface.
    /// </summary>
    public string Interface { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets lifetime.
    /// </summary>
    public string Lifetime { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets domain objects.
    /// </summary>
    public List<string> DomainObjects { get; set; } = [];
    /// <summary>
    /// Gets or sets methods.
    /// </summary>
    public List<string> Methods { get; set; } = [];
    /// <summary>
    /// Gets or sets controllers.
    /// </summary>
    public List<string> Controllers { get; set; } = [];
}

/// <summary>
/// Represents a business object context snapshot.
/// </summary>
public sealed class BusinessObjectContextSnapshot
{
    /// <summary>
    /// Gets or sets generated UTC.
    /// </summary>
    public DateTimeOffset GeneratedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets domain objects.
    /// </summary>
    public List<BusinessObjectDescriptor> DomainObjects { get; set; } = [];
    /// <summary>
    /// Gets or sets services.
    /// </summary>
    public List<ServiceApiDescriptor> Services { get; set; } = [];
    /// <summary>
    /// Gets or sets controller routes.
    /// </summary>
    public List<string> ControllerRoutes { get; set; } = [];
}

/// <summary>
/// Represents an API surface descriptor.
/// </summary>
public sealed class ApiSurfaceDescriptor
{
    /// <summary>
    /// Gets or sets controller.
    /// </summary>
    public string Controller { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets routes.
    /// </summary>
    public List<string> Routes { get; set; } = [];
    /// <summary>
    /// Gets or sets methods.
    /// </summary>
    public List<ApiSurfaceMethodDescriptor> Methods { get; set; } = [];
    /// <summary>
    /// Gets or sets service contracts.
    /// </summary>
    public List<string> ServiceContracts { get; set; } = [];
}

/// <summary>
/// Represents an API surface method descriptor.
/// </summary>
public sealed class ApiSurfaceMethodDescriptor
{
    /// <summary>
    /// Gets or sets method name.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets HTTP method.
    /// </summary>
    public string HttpMethod { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets route.
    /// </summary>
    public string Route { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets is read only.
    /// </summary>
    public bool IsReadOnly { get; set; }
}

/// <summary>
/// Represents a browser runtime template request.
/// </summary>
public sealed record BrowserRuntimeTemplateRequest(string Payload);

/// <summary>
/// Represents a polygon resample request.
/// </summary>
public sealed class PolygonResampleRequest
{
    /// <summary>
    /// Gets or sets points.
    /// </summary>
    public List<MediaFramePoint> Points { get; set; } = [];
    /// <summary>
    /// Gets or sets count.
    /// </summary>
    public int Count { get; set; } = 16;
}
