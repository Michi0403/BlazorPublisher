using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using PublisherStudio.Domain;
using PublisherStudio.Services.Automation;

namespace PublisherStudio.Controllers;

/// <summary>MVC adapter for describing the public HTTP surface without leaking MVC into reusable services.</summary>
public sealed class ApiSurfaceCatalogService : IApiSurfaceCatalogService
{
    private readonly Assembly _assembly = typeof(ApiSurfaceCatalogService).Assembly;

    public IReadOnlyList<ApiSurfaceDescriptor> GetSurfaces() => _assembly.GetExportedTypes()
        .Where(type => typeof(ControllerBase).IsAssignableFrom(type))
        .OrderBy(type => type.FullName)
        .Select(CreateDescriptor)
        .ToList()
        .AsReadOnly();

    private ApiSurfaceDescriptor CreateDescriptor(Type controller)
    {
        var root = controller.GetCustomAttribute<RouteAttribute>()?.Template ?? controller.Name;
        var routes = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(method => CombineRoute(root, controller.Name, method.GetCustomAttributes<HttpMethodAttribute>().FirstOrDefault()?.Template ?? method.Name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(route => route)
            .ToList();
        var contracts = controller.GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.ParameterType.FullName ?? parameter.ParameterType.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name)
            .ToList();
        return new ApiSurfaceDescriptor { Controller = controller.Name, Routes = routes, ServiceContracts = contracts };
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
