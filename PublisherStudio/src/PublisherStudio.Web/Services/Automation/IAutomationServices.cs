using PublisherStudio.Domain;

namespace PublisherStudio.Services.Automation;

public interface IUserInputAutomationService
{
    BrowserAutomationCommand Enqueue(BrowserAutomationCommand command);
    IReadOnlyList<BrowserAutomationCommand> GetAll();
    IReadOnlyList<BrowserAutomationCommand> ClaimPending(int maximum = 25);
    bool Complete(Guid id, AutomationCompletion completion);
    bool Cancel(Guid id);
}

public interface IScreenshotCaptureService
{
    BrowserScreenshotRequest Enqueue(BrowserScreenshotRequest request);
    IReadOnlyList<BrowserScreenshotRequest> GetAll();
    IReadOnlyList<BrowserScreenshotRequest> ClaimPending(int maximum = 5);
    bool Complete(Guid id, ScreenshotCompletion completion);
    bool TryGet(Guid id, out BrowserScreenshotRequest request);
    bool Cancel(Guid id);
}

public interface IBusinessObjectContextService
{
    BusinessObjectContextSnapshot CreateSnapshot();
}

public interface IApiSurfaceCatalogService
{
    IReadOnlyList<ApiSurfaceDescriptor> GetSurfaces();
}
