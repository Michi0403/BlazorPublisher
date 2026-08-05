using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PublisherStudio.Diagnostics;

/// <summary>
/// Provides controller request logging filter operations.
/// </summary>
public sealed class ControllerRequestLoggingFilter(
    ILogger<ControllerRequestLoggingFilter> logger) : IAsyncActionFilter
{
    /// <summary>
    /// Runs the on action execution async operation.
    /// </summary>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        var routeValues = context.ActionDescriptor.RouteValues;
        var controller = routeValues.TryGetValue("controller", out var controllerValue) && !string.IsNullOrWhiteSpace(controllerValue)
            ? controllerValue
            : "unknown";
        var action = routeValues.TryGetValue("action", out var actionValue) && !string.IsNullOrWhiteSpace(actionValue)
            ? actionValue
            : "unknown";
        var request = context.HttpContext.Request;
        logger.LogInformation(
            "Controller action {Controller}.{Action} started for {Method} {Path}.",
            controller,
            action,
            request.Method,
            request.Path);

        try
        {
            var executed = await next();
            if (executed.Exception is not null && !executed.ExceptionHandled)
            {
                logger.LogError(
                    executed.Exception,
                    "Controller action {Controller}.{Action} failed after {ElapsedMilliseconds} ms.",
                    controller,
                    action,
                    stopwatch.ElapsedMilliseconds);
                return;
            }

            logger.LogInformation(
                "Controller action {Controller}.{Action} completed with status {StatusCode} after {ElapsedMilliseconds} ms.",
                controller,
                action,
                context.HttpContext.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (context.HttpContext.RequestAborted.IsCancellationRequested)
        {
#if DEBUG
            logger.LogInformation(
                "Controller action {Controller}.{Action} was cancelled because the client disconnected in a Debug build.",
                controller,
                action);
#endif
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Controller action {Controller}.{Action} threw after {ElapsedMilliseconds} ms.",
                controller,
                action,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
