using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using PublisherStudio.Domain;
using PublisherStudio.Services.Automation;

namespace PublisherStudio.Controllers;

/// <summary>MVC adapter for describing the public HTTP surface without leaking MVC into reusable services.</summary>
public sealed class ApiSurfaceCatalogService(ILogger<ApiSurfaceCatalogService> logger) : IApiSurfaceCatalogService
{
    private readonly Assembly assembly = typeof(ApiSurfaceCatalogService).Assembly;

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
