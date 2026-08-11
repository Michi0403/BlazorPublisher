using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Automation;

/// <summary>
/// Defines the user input automation service contract.
/// </summary>
public interface IUserInputAutomationService
{
    /// <summary>
    /// Runs the enqueue operation.
    /// </summary>
    BrowserAutomationCommand Enqueue(BrowserAutomationCommand command);
    /// <summary>
    /// Gets all.
    /// </summary>
    IReadOnlyList<BrowserAutomationCommand> GetAll();
    /// <summary>
    /// Runs the claim pending operation.
    /// </summary>
    IReadOnlyList<BrowserAutomationCommand> ClaimPending(int maximum = 25);
    /// <summary>
    /// Runs the complete operation.
    /// </summary>
    bool Complete(Guid id, AutomationCompletion completion);
    /// <summary>
    /// Determines whether cel.
    /// </summary>
    bool Cancel(Guid id);
}

/// <summary>
/// Defines the screenshot capture service contract.
/// </summary>
public interface IScreenshotCaptureService
{
    /// <summary>
    /// Runs the enqueue operation.
    /// </summary>
    BrowserScreenshotRequest Enqueue(BrowserScreenshotRequest request);
    /// <summary>
    /// Gets all.
    /// </summary>
    IReadOnlyList<BrowserScreenshotRequest> GetAll();
    /// <summary>
    /// Runs the claim pending operation.
    /// </summary>
    IReadOnlyList<BrowserScreenshotRequest> ClaimPending(int maximum = 5);
    /// <summary>
    /// Runs the complete operation.
    /// </summary>
    bool Complete(Guid id, ScreenshotCompletion completion);
    /// <summary>
    /// Attempts to get.
    /// </summary>
    bool TryGet(Guid id, out BrowserScreenshotRequest request);
    /// <summary>
    /// Determines whether cel.
    /// </summary>
    bool Cancel(Guid id);
}

/// <summary>
/// Defines the business object context service contract.
/// </summary>
public interface IBusinessObjectContextService
{
    /// <summary>
    /// Creates snapshot.
    /// </summary>
    BusinessObjectContextSnapshot CreateSnapshot();
}

/// <summary>
/// Defines the API surface catalog service contract.
/// </summary>
public interface IApiSurfaceCatalogService
{
    /// <summary>
    /// Gets surfaces.
    /// </summary>
    IReadOnlyList<ApiSurfaceDescriptor> GetSurfaces();
}
