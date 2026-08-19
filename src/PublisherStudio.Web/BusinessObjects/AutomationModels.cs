namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Defines the supported browser automation command kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum BrowserAutomationCommandKind
{
    /// <summary>
    /// Selects the click option for <see cref="BrowserAutomationCommandKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Click, DoubleClick, ContextMenu, MouseMove, MouseDown, MouseUp, Wheel, Focus, Blur, TypeText, SetValue, KeyDown, KeyUp, KeyPress
}

/// <summary>
/// Defines the supported automation request status values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum AutomationRequestStatus { Pending, Claimed, Completed, Failed, Cancelled }

/// <summary>
/// Represents a browser automation command application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class BrowserAutomationCommand
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this browser automation command instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="BrowserAutomationCommand"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the kind value that forms part of the browser automation command state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="BrowserAutomationCommand"/>.</value>
    public BrowserAutomationCommandKind Kind { get; set; }
    /// <summary>
    /// Gets or sets the selector value that forms part of the browser automation command state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selector value exposed by <see cref="BrowserAutomationCommand"/>.</value>
    public string Selector { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the text value that forms part of the browser automation command state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The text value exposed by <see cref="BrowserAutomationCommand"/>.</value>
    public string Text { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this browser automation command instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="BrowserAutomationCommand"/>.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the code value that forms part of the browser automation command state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The code value exposed by <see cref="BrowserAutomationCommand"/>.</value>
    public string Code { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the button value that forms part of the browser automation command state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The button value exposed by <see cref="BrowserAutomationCommand"/>.</value>
    public int Button { get; set; }
    /// <summary>
    /// Gets or sets the x value that forms part of the browser automation command state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The x value exposed by <see cref="BrowserAutomationCommand"/>.</value>
    public double X { get; set; }
    /// <summary>
    /// Gets or sets the y value that forms part of the browser automation command state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The y value exposed by <see cref="BrowserAutomationCommand"/>.</value>
    public double Y { get; set; }
    /// <summary>
    /// Gets or sets the delta x value that forms part of the browser automation command state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The delta x value exposed by <see cref="BrowserAutomationCommand"/>.</value>
    public double DeltaX { get; set; }
    /// <summary>
    /// Gets or sets the delta y value that forms part of the browser automation command state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The delta y value exposed by <see cref="BrowserAutomationCommand"/>.</value>
    public double DeltaY { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether ctrl key applies to the browser automation command state.
    /// </summary>
    /// <value>The ctrl key value exposed by <see cref="BrowserAutomationCommand"/>.</value>
    public bool CtrlKey { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether shift key applies to the browser automation command state.
    /// </summary>
    /// <value>The shift key value exposed by <see cref="BrowserAutomationCommand"/>.</value>
    public bool ShiftKey { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether alt key applies to the browser automation command state.
    /// </summary>
    /// <value>The alt key value exposed by <see cref="BrowserAutomationCommand"/>.</value>
    public bool AltKey { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether meta key applies to the browser automation command state.
    /// </summary>
    /// <value>The meta key value exposed by <see cref="BrowserAutomationCommand"/>.</value>
    public bool MetaKey { get; set; }
    /// <summary>
    /// Gets or sets the created UTC associated with this browser automation command state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created UTC value exposed by <see cref="BrowserAutomationCommand"/>.</value>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets the completed UTC associated with this browser automation command state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The completed UTC value exposed by <see cref="BrowserAutomationCommand"/>.</value>
    public DateTimeOffset? CompletedUtc { get; set; }
    /// <summary>
    /// Gets or sets the status value that forms part of the browser automation command state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="BrowserAutomationCommand"/>.</value>
    public AutomationRequestStatus Status { get; set; } = AutomationRequestStatus.Pending;
    /// <summary>
    /// Gets or sets the result value that forms part of the browser automation command state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The result value exposed by <see cref="BrowserAutomationCommand"/>.</value>
    public string Result { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the error value that forms part of the browser automation command state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The error value exposed by <see cref="BrowserAutomationCommand"/>.</value>
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// Represents the input contract for browser screenshot, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class BrowserScreenshotRequest
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this browser screenshot instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="BrowserScreenshotRequest"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the selector value that forms part of the browser screenshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selector value exposed by <see cref="BrowserScreenshotRequest"/>.</value>
    public string Selector { get; set; } = "body";
    /// <summary>
    /// Gets or sets the format value that forms part of the browser screenshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The format value exposed by <see cref="BrowserScreenshotRequest"/>.</value>
    public string Format { get; set; } = "png";
    /// <summary>
    /// Gets or sets the quality value that forms part of the browser screenshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The quality value exposed by <see cref="BrowserScreenshotRequest"/>.</value>
    public double Quality { get; set; } = .92;
    /// <summary>
    /// Gets or sets the scale value that forms part of the browser screenshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The scale value exposed by <see cref="BrowserScreenshotRequest"/>.</value>
    public double Scale { get; set; } = 1;
    /// <summary>
    /// Gets or sets a value indicating whether metadata applies to the browser screenshot state.
    /// </summary>
    /// <value>The include metadata value exposed by <see cref="BrowserScreenshotRequest"/>.</value>
    public bool IncludeMetadata { get; set; } = true;
    /// <summary>
    /// Gets or sets the created UTC associated with this browser screenshot state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created UTC value exposed by <see cref="BrowserScreenshotRequest"/>.</value>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets the completed UTC associated with this browser screenshot state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The completed UTC value exposed by <see cref="BrowserScreenshotRequest"/>.</value>
    public DateTimeOffset? CompletedUtc { get; set; }
    /// <summary>
    /// Gets or sets the status value that forms part of the browser screenshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="BrowserScreenshotRequest"/>.</value>
    public AutomationRequestStatus Status { get; set; } = AutomationRequestStatus.Pending;
    /// <summary>
    /// Gets or sets the data URL that identifies the network or application endpoint associated with this browser screenshot state.
    /// </summary>
    /// <value>The data URL value exposed by <see cref="BrowserScreenshotRequest"/>.</value>
    public string DataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the pixel width value that forms part of the browser screenshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The pixel width value exposed by <see cref="BrowserScreenshotRequest"/>.</value>
    public int PixelWidth { get; set; }
    /// <summary>
    /// Gets or sets the pixel height value that forms part of the browser screenshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The pixel height value exposed by <see cref="BrowserScreenshotRequest"/>.</value>
    public int PixelHeight { get; set; }
    /// <summary>
    /// Gets or sets the error value that forms part of the browser screenshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The error value exposed by <see cref="BrowserScreenshotRequest"/>.</value>
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// Represents an automation completion application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Result">Result value supplied to the automation completion operation and used when producing its result.</param>
/// <param name="Error">Error value supplied to the automation completion operation and used when producing its result.</param>
public sealed record AutomationCompletion(string Result = "", string Error = "");
/// <summary>
/// Represents a screenshot completion application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="DataUrl">Data url value supplied to the screenshot completion operation and used when producing its result.</param>
/// <param name="PixelWidth">Pixel width value supplied to the screenshot completion operation and used when producing its result.</param>
/// <param name="PixelHeight">Pixel height value supplied to the screenshot completion operation and used when producing its result.</param>
/// <param name="Error">Error value supplied to the screenshot completion operation and used when producing its result.</param>
public sealed record ScreenshotCompletion(string DataUrl, int PixelWidth, int PixelHeight, string Error = "");

/// <summary>
/// Represents business object state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
public sealed class BusinessObjectDescriptor
{
    /// <summary>
    /// Gets or sets the name value that forms part of the business object state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="BusinessObjectDescriptor"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the full name value that forms part of the business object state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The full name value exposed by <see cref="BusinessObjectDescriptor"/>.</value>
    public string FullName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the kind value that forms part of the business object state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="BusinessObjectDescriptor"/>.</value>
    public string Kind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the properties collection maintained or exposed by this business object instance for downstream processing.
    /// </summary>
    /// <value>The properties value exposed by <see cref="BusinessObjectDescriptor"/>.</value>
    public List<string> Properties { get; set; } = [];
}

/// <summary>
/// Represents service API state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
public sealed class ServiceApiDescriptor
{
    /// <summary>
    /// Gets or sets the service value that forms part of the service API state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The service value exposed by <see cref="ServiceApiDescriptor"/>.</value>
    public string Service { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the interface value that forms part of the service API state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The interface value exposed by <see cref="ServiceApiDescriptor"/>.</value>
    public string Interface { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the lifetime value that forms part of the service API state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The lifetime value exposed by <see cref="ServiceApiDescriptor"/>.</value>
    public string Lifetime { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the domain objects collection maintained or exposed by this service API instance for downstream processing.
    /// </summary>
    /// <value>The domain objects value exposed by <see cref="ServiceApiDescriptor"/>.</value>
    public List<string> DomainObjects { get; set; } = [];
    /// <summary>
    /// Gets or sets the methods collection maintained or exposed by this service API instance for downstream processing.
    /// </summary>
    /// <value>The methods value exposed by <see cref="ServiceApiDescriptor"/>.</value>
    public List<string> Methods { get; set; } = [];
    /// <summary>
    /// Gets or sets the controllers collection maintained or exposed by this service API instance for downstream processing.
    /// </summary>
    /// <value>The controllers value exposed by <see cref="ServiceApiDescriptor"/>.</value>
    public List<string> Controllers { get; set; } = [];
}

/// <summary>
/// Represents a business object context snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class BusinessObjectContextSnapshot
{
    /// <summary>
    /// Gets or sets the generated UTC associated with this business object context snapshot state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The generated UTC value exposed by <see cref="BusinessObjectContextSnapshot"/>.</value>
    public DateTimeOffset GeneratedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets the domain objects collection maintained or exposed by this business object context snapshot instance for downstream processing.
    /// </summary>
    /// <value>The domain objects value exposed by <see cref="BusinessObjectContextSnapshot"/>.</value>
    public List<BusinessObjectDescriptor> DomainObjects { get; set; } = [];
    /// <summary>
    /// Gets or sets the services collection maintained or exposed by this business object context snapshot instance for downstream processing.
    /// </summary>
    /// <value>The services value exposed by <see cref="BusinessObjectContextSnapshot"/>.</value>
    public List<ServiceApiDescriptor> Services { get; set; } = [];
    /// <summary>
    /// Gets or sets the controller routes collection maintained or exposed by this business object context snapshot instance for downstream processing.
    /// </summary>
    /// <value>The controller routes value exposed by <see cref="BusinessObjectContextSnapshot"/>.</value>
    public List<string> ControllerRoutes { get; set; } = [];
}

/// <summary>
/// Represents API surface state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
public sealed class ApiSurfaceDescriptor
{
    /// <summary>
    /// Gets or sets the controller value that forms part of the API surface state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The controller value exposed by <see cref="ApiSurfaceDescriptor"/>.</value>
    public string Controller { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the routes collection maintained or exposed by this API surface instance for downstream processing.
    /// </summary>
    /// <value>The routes value exposed by <see cref="ApiSurfaceDescriptor"/>.</value>
    public List<string> Routes { get; set; } = [];
    /// <summary>
    /// Gets or sets the methods collection maintained or exposed by this API surface instance for downstream processing.
    /// </summary>
    /// <value>The methods value exposed by <see cref="ApiSurfaceDescriptor"/>.</value>
    public List<ApiSurfaceMethodDescriptor> Methods { get; set; } = [];
    /// <summary>
    /// Gets or sets the service contracts collection maintained or exposed by this API surface instance for downstream processing.
    /// </summary>
    /// <value>The service contracts value exposed by <see cref="ApiSurfaceDescriptor"/>.</value>
    public List<string> ServiceContracts { get; set; } = [];
}

/// <summary>
/// Represents API surface method state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
public sealed class ApiSurfaceMethodDescriptor
{
    /// <summary>
    /// Gets or sets the method name value that forms part of the API surface method state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The method name value exposed by <see cref="ApiSurfaceMethodDescriptor"/>.</value>
    public string MethodName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the HTTP method value that forms part of the API surface method state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The HTTP method value exposed by <see cref="ApiSurfaceMethodDescriptor"/>.</value>
    public string HttpMethod { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the route value that forms part of the API surface method state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The route value exposed by <see cref="ApiSurfaceMethodDescriptor"/>.</value>
    public string Route { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether read only applies to the API surface method state.
    /// </summary>
    /// <value>The is read only value exposed by <see cref="ApiSurfaceMethodDescriptor"/>.</value>
    public bool IsReadOnly { get; set; }
}

/// <summary>
/// Represents the input contract for browser runtime template, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
/// <param name="Payload">Payload value supplied to the browser runtime template operation and used when producing its result.</param>
public sealed record BrowserRuntimeTemplateRequest(string Payload);

/// <summary>
/// Represents the input contract for polygon resample, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class PolygonResampleRequest
{
    /// <summary>
    /// Gets or sets the points collection maintained or exposed by this polygon resample instance for downstream processing.
    /// </summary>
    /// <value>The points value exposed by <see cref="PolygonResampleRequest"/>.</value>
    public List<MediaFramePoint> Points { get; set; } = [];
    /// <summary>
    /// Gets or sets the count that quantifies the associated polygon resample data.
    /// </summary>
    /// <value>The count value exposed by <see cref="PolygonResampleRequest"/>.</value>
    public int Count { get; set; } = 16;
}
