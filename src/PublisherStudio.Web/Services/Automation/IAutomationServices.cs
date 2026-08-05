using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Automation;

/// <summary>
/// Defines the user input automation service contract.
/// </summary>
public interface IUserInputAutomationService
{
    BrowserAutomationCommand Enqueue(BrowserAutomationCommand command);
    IReadOnlyList<BrowserAutomationCommand> GetAll();
    IReadOnlyList<BrowserAutomationCommand> ClaimPending(int maximum = 25);
    bool Complete(Guid id, AutomationCompletion completion);
    bool Cancel(Guid id);
}

/// <summary>
/// Defines the screenshot capture service contract.
/// </summary>
public interface IScreenshotCaptureService
{
    BrowserScreenshotRequest Enqueue(BrowserScreenshotRequest request);
    IReadOnlyList<BrowserScreenshotRequest> GetAll();
    IReadOnlyList<BrowserScreenshotRequest> ClaimPending(int maximum = 5);
    bool Complete(Guid id, ScreenshotCompletion completion);
    bool TryGet(Guid id, out BrowserScreenshotRequest request);
    bool Cancel(Guid id);
}

/// <summary>
/// Defines the business object context service contract.
/// </summary>
public interface IBusinessObjectContextService
{
    BusinessObjectContextSnapshot CreateSnapshot();
}

/// <summary>
/// Defines the API surface catalog service contract.
/// </summary>
public interface IApiSurfaceCatalogService
{
    IReadOnlyList<ApiSurfaceDescriptor> GetSurfaces();
}
