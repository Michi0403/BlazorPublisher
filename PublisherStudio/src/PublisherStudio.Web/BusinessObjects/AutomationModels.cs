namespace PublisherStudio.BusinessObjects;

public enum BrowserAutomationCommandKind
{
    Click, DoubleClick, ContextMenu, MouseMove, MouseDown, MouseUp, Wheel, Focus, Blur, TypeText, SetValue, KeyDown, KeyUp, KeyPress
}

public enum AutomationRequestStatus { Pending, Claimed, Completed, Failed, Cancelled }

public sealed class BrowserAutomationCommand
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public BrowserAutomationCommandKind Kind { get; set; }
    public string Selector { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Button { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double DeltaX { get; set; }
    public double DeltaY { get; set; }
    public bool CtrlKey { get; set; }
    public bool ShiftKey { get; set; }
    public bool AltKey { get; set; }
    public bool MetaKey { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedUtc { get; set; }
    public AutomationRequestStatus Status { get; set; } = AutomationRequestStatus.Pending;
    public string Result { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

public sealed class BrowserScreenshotRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Selector { get; set; } = "body";
    public string Format { get; set; } = "png";
    public double Quality { get; set; } = .92;
    public double Scale { get; set; } = 1;
    public bool IncludeMetadata { get; set; } = true;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedUtc { get; set; }
    public AutomationRequestStatus Status { get; set; } = AutomationRequestStatus.Pending;
    public string DataUrl { get; set; } = string.Empty;
    public int PixelWidth { get; set; }
    public int PixelHeight { get; set; }
    public string Error { get; set; } = string.Empty;
}

public sealed record AutomationCompletion(string Result = "", string Error = "");
public sealed record ScreenshotCompletion(string DataUrl, int PixelWidth, int PixelHeight, string Error = "");

public sealed class BusinessObjectDescriptor
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public List<string> Properties { get; set; } = [];
}

public sealed class ServiceApiDescriptor
{
    public string Service { get; set; } = string.Empty;
    public string Interface { get; set; } = string.Empty;
    public string Lifetime { get; set; } = string.Empty;
    public List<string> DomainObjects { get; set; } = [];
    public List<string> Methods { get; set; } = [];
    public List<string> Controllers { get; set; } = [];
}

public sealed class BusinessObjectContextSnapshot
{
    public DateTimeOffset GeneratedUtc { get; set; } = DateTimeOffset.UtcNow;
    public List<BusinessObjectDescriptor> DomainObjects { get; set; } = [];
    public List<ServiceApiDescriptor> Services { get; set; } = [];
    public List<string> ControllerRoutes { get; set; } = [];
}

public sealed class ApiSurfaceDescriptor
{
    public string Controller { get; set; } = string.Empty;
    public List<string> Routes { get; set; } = [];
    public List<ApiSurfaceMethodDescriptor> Methods { get; set; } = [];
    public List<string> ServiceContracts { get; set; } = [];
}

public sealed class ApiSurfaceMethodDescriptor
{
    public string MethodName { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public bool IsReadOnly { get; set; }
}

public sealed record BrowserRuntimeTemplateRequest(string Payload);

public sealed class PolygonResampleRequest
{
    public List<MediaFramePoint> Points { get; set; } = [];
    public int Count { get; set; } = 16;
}
