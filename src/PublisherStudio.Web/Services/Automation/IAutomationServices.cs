using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Automation;

/// <summary>
/// Defines the contract for user input automation behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IUserInputAutomationService
{
    /// <summary>
    /// Performs enqueue as part of the user input automation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="command">Command value supplied to the user input automation operation and used when producing its result.</param>
    /// <returns>The browser automation command produced by the operation.</returns>
    BrowserAutomationCommand Enqueue(BrowserAutomationCommand command);
    /// <summary>
    /// Retrieves all as part of the user input automation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<BrowserAutomationCommand> GetAll();
    /// <summary>
    /// Performs claim pending as part of the user input automation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="maximum">Maximum value supplied to the user input automation operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<BrowserAutomationCommand> ClaimPending(int maximum = 25);
    /// <summary>
    /// Performs complete as part of the user input automation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="completion">Completion value supplied to the user input automation operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool Complete(Guid id, AutomationCompletion completion);
    /// <summary>
    /// Determines whether cel as part of the user input automation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool Cancel(Guid id);
}

/// <summary>
/// Defines the contract for screenshot capture behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IScreenshotCaptureService
{
    /// <summary>
    /// Performs enqueue as part of the screenshot capture service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The browser screenshot request produced by the operation.</returns>
    BrowserScreenshotRequest Enqueue(BrowserScreenshotRequest request);
    /// <summary>
    /// Retrieves all as part of the screenshot capture service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<BrowserScreenshotRequest> GetAll();
    /// <summary>
    /// Performs claim pending as part of the screenshot capture service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="maximum">Maximum value supplied to the screenshot capture operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<BrowserScreenshotRequest> ClaimPending(int maximum = 5);
    /// <summary>
    /// Performs complete as part of the screenshot capture service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="completion">Completion value supplied to the screenshot capture operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool Complete(Guid id, ScreenshotCompletion completion);
    /// <summary>
    /// Attempts to get as part of the screenshot capture service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool TryGet(Guid id, out BrowserScreenshotRequest request);
    /// <summary>
    /// Determines whether cel as part of the screenshot capture service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool Cancel(Guid id);
}

/// <summary>
/// Defines the contract for business object context behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IBusinessObjectContextService
{
    /// <summary>
    /// Creates snapshot as part of the business object context service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The business object context snapshot produced by the operation.</returns>
    BusinessObjectContextSnapshot CreateSnapshot();
}

/// <summary>
/// Defines the contract for API surface catalog behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IApiSurfaceCatalogService
{
    /// <summary>
    /// Retrieves surfaces as part of the API surface catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<ApiSurfaceDescriptor> GetSurfaces();
}
