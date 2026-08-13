using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Automation;

namespace PublisherStudio.Controllers;

/// <summary>MVC adapter for describing the public HTTP surface without leaking MVC into reusable services.</summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ApiSurfaceCatalogService(ILogger<ApiSurfaceCatalogService> logger) : IApiSurfaceCatalogService
{
    /// <summary>
    /// Stores the internal assembly state used by <see cref="ApiSurfaceCatalogService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Assembly assembly = typeof(ApiSurfaceCatalogService).Assembly;

    /// <summary>
    /// Retrieves surfaces as part of the API surface catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<ApiSurfaceDescriptor> GetSurfaces()
    {
        try
        {
            var result = assembly.GetExportedTypes()
                .Where(type => typeof(ControllerBase).IsAssignableFrom(type))
                .OrderBy(type => type.FullName)
                .Select(CreateDescriptor)
                .ToList()
                .AsReadOnly();
            logger.LogInformation("Discovered {ControllerCount} Publisher Studio ASP.NET Core controller surface(s).", result.Count);
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not discover the Publisher Studio ASP.NET Core controller surface.");
            throw;
        }
    }

    /// <summary>
    /// Creates descriptor as part of the API surface catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="controller">Controller value supplied to the API surface catalog operation and used when producing its result.</param>
    /// <returns>The API surface descriptor produced by the operation.</returns>
    private ApiSurfaceDescriptor CreateDescriptor(Type controller)
    {
        try
        {
            var root = controller.GetCustomAttribute<RouteAttribute>()?.Template ?? controller.Name;
            var methods = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>()
                    .SelectMany(attribute => attribute.HttpMethods.Select(httpMethod => new ApiSurfaceMethodDescriptor
                    {
                        MethodName = method.Name,
                        HttpMethod = httpMethod,
                        Route = CombineRoute(root, controller.Name, attribute.Template ?? string.Empty),
                        IsReadOnly = string.Equals(httpMethod, "GET", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(httpMethod, "HEAD", StringComparison.OrdinalIgnoreCase)
                    })))
                .GroupBy(method => $"{method.MethodName}|{method.HttpMethod}|{method.Route}", StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(method => method.Route, StringComparer.OrdinalIgnoreCase)
                .ThenBy(method => method.HttpMethod, StringComparer.OrdinalIgnoreCase)
                .ThenBy(method => method.MethodName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var routes = methods
                .Select(method => method.Route)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(route => route, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var contracts = controller.GetConstructors()
                .SelectMany(constructor => constructor.GetParameters())
                .Select(parameter => parameter.ParameterType.FullName ?? parameter.ParameterType.Name)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();
            logger.LogTrace(
                "Discovered Publisher Studio controller {ControllerName} with {MethodCount} HTTP method(s) across {RouteCount} route(s).",
                controller.Name,
                methods.Count,
                routes.Count);
            return new ApiSurfaceDescriptor
            {
                Controller = controller.Name,
                Routes = routes,
                Methods = methods,
                ServiceContracts = contracts
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not describe Publisher Studio controller {ControllerName}.", controller.FullName ?? controller.Name);
            throw;
        }
    }

    /// <summary>
    /// Performs combine route as part of the API surface catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="root">Root value supplied to the API surface catalog operation and used when producing its result.</param>
    /// <param name="controllerName">Controller name value supplied to the API surface catalog operation and used when producing its result.</param>
    /// <param name="action">Action value supplied to the API surface catalog operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string CombineRoute(string root, string controllerName, string action)
    {
        var routeName = controllerName.EndsWith("Controller", StringComparison.Ordinal)
            ? controllerName[..^"Controller".Length]
            : controllerName;
        var normalizedRoot = root.Replace("[controller]", routeName, StringComparison.OrdinalIgnoreCase).Trim('/');
        var normalizedAction = action.Trim('/');
        return string.IsNullOrWhiteSpace(normalizedAction) ? normalizedRoot : $"{normalizedRoot}/{normalizedAction}";
    }
}
