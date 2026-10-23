using System.Text.Json;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Automation;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>Discovers only the modules mounted in the permission-enabled PublisherStudio browser tab.</summary>
/// <param name="automation">Existing browser automation queue owned by the connected frontend.</param>
/// <param name="logger">Structured diagnostic logger.</param>
public sealed class PanelModuleInspectOrganicHandler(
    IUserInputAutomationService automation,
    ILogger<PanelModuleInspectOrganicHandler> logger) : IOrganicExecutableCapabilityHandler
{
    /// <summary>Gets the catalog identifier for live module-function discovery.</summary>
    public string CapabilityKey => "publisher.panel.modules.inspect";

    /// <summary>Queues a read-only module/method inventory through the existing opt-in browser automation channel.</summary>
    /// <param name="envelope">Already permission-checked organic request.</param>
    /// <param name="cancellationToken">Cancels the request before queueing.</param>
    /// <returns>Correlation handle for publisher.input.result; no publication data is modified.</returns>
    public Task<object> ExecuteAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var queued = automation.Enqueue(new BrowserAutomationCommand { Kind = BrowserAutomationCommandKind.InspectModules });
            logger.LogInformation("Queued organic Panel/Div module inspection {RequestId}.", queued.Id);
            object result = new { queued.Id, queued.Status, ResultCapability = "publisher.input.result", RequiresEnabledBrowserTab = true };
            return Task.FromResult(result);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Panel/Div organic module inspection failed.");
            throw;
        }
    }
}

/// <summary>Runs an approved module function through the same browser runtime used by exported standalone pages.</summary>
/// <param name="automation">Existing browser automation queue and consent boundary.</param>
/// <param name="logger">Structured diagnostic logger.</param>
public sealed class PanelModuleInvokeOrganicHandler(
    IUserInputAutomationService automation,
    ILogger<PanelModuleInvokeOrganicHandler> logger) : IOrganicExecutableCapabilityHandler
{
    /// <summary>Gets the catalog identifier for invoking a mounted module function.</summary>
    public string CapabilityKey => "publisher.panel.module.invoke";

    /// <summary>Queues one module function after existing 1-Wire permissions and the frontend approval policy have accepted it.</summary>
    /// <param name="envelope">Already approved 1-Wire work envelope containing typed parameters.</param>
    /// <param name="cancellationToken">Cancels before queueing.</param>
    /// <returns>A queue receipt, not a premature success claim; publisher.input.result contains execution outcome.</returns>
    public Task<object> ExecuteAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (envelope.Properties is null ||
                !envelope.Properties.TryGetValue("Parameters", out var parameters) ||
                parameters.ValueKind != JsonValueKind.Object)
                throw new ArgumentException("Module invocation requires typed Parameters.");
            if (!parameters.TryGetProperty("elementId", out var idProperty) ||
                idProperty.ValueKind != JsonValueKind.String ||
                !Guid.TryParse(idProperty.GetString(), out var elementId))
                throw new ArgumentException("A valid panel module elementId is required.");
            if (!parameters.TryGetProperty("method", out var methodProperty) ||
                methodProperty.ValueKind != JsonValueKind.String)
                throw new ArgumentException("A module method is required.");
            var method = methodProperty.GetString()?.Trim() ?? string.Empty;
            if (method.Length is < 2 or > 40 || !method.All(char.IsLetter))
                throw new ArgumentException("The module method must be a simple bounded method name.");
            var valueJson = parameters.TryGetProperty("value", out var valueProperty)
                ? valueProperty.GetRawText() : "null";
            if (valueJson.Length > 4096)
                throw new ArgumentException("The module method argument exceeds the bounded payload limit.");
            var queued = automation.Enqueue(new BrowserAutomationCommand
            {
                Kind = BrowserAutomationCommandKind.InvokeModule,
                Selector = elementId.ToString("D"), Key = method, Text = valueJson
            });
            logger.LogInformation("Queued approved Panel/Div module function {Method} on element {ElementId}, request {RequestId}.",
                method, elementId, queued.Id);
            object result = new { queued.Id, queued.Status, ResultCapability = "publisher.input.result", RequiresEnabledBrowserTab = true };
            return Task.FromResult(result);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Approved Panel/Div organic module invocation could not be queued.");
            throw;
        }
    }
}
