using System.Reflection;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Automation;

/// <summary>
/// Provides business object context service operations.
/// </summary>
public sealed class BusinessObjectContextService(
    IServiceArchitectureRegistry architecture,
    IApiSurfaceCatalogService apiSurfaceCatalog) : IBusinessObjectContextService
{
    private readonly Assembly _assembly = typeof(BusinessObjectContextService).Assembly;

    /// <summary>
    /// Creates snapshot.
    /// </summary>
    public BusinessObjectContextSnapshot CreateSnapshot()
    {
    try
    {
            var domainTypes = _assembly.GetExportedTypes()
                .Where(type => type.Namespace?.StartsWith("PublisherStudio.BusinessObjects", StringComparison.Ordinal) == true)
                .OrderBy(type => type.FullName)
                .Select(type => new BusinessObjectDescriptor
                {
                    Name = type.Name,
                    FullName = type.FullName ?? type.Name,
                    Kind = type.IsEnum ? "enum" : type.IsInterface ? "interface" : type.IsValueType ? "value" : "class",
                    Properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Select(property => $"{property.Name}: {FriendlyName(property.PropertyType)}")
                        .ToList()
                }).ToList();

            var apiSurfaces = apiSurfaceCatalog.GetSurfaces();
            var services = architecture.Descriptors.Select(descriptor =>
            {
                var contract = descriptor.InterfaceType ?? descriptor.ImplementationType;
                var contractNames = new HashSet<string>(StringComparer.Ordinal)
                {
                    contract.FullName ?? contract.Name,
                    descriptor.ImplementationType.FullName ?? descriptor.ImplementationType.Name
                };
                var relatedControllers = apiSurfaces
                    .Where(surface => surface.ServiceContracts.Any(contractNames.Contains))
                    .Select(surface => surface.Controller)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(name => name)
                    .ToList();
                var domainObjects = descriptor.ImplementationType
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType))
                    .SelectMany(UnwrapTypes)
                    .Where(type => type.Namespace?.StartsWith("PublisherStudio.BusinessObjects", StringComparison.Ordinal) == true)
                    .Select(type => type.Name)
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList();
                return new ServiceApiDescriptor
                {
                    Service = descriptor.ImplementationType.FullName ?? descriptor.ImplementationType.Name,
                    Interface = descriptor.InterfaceType?.FullName ?? string.Empty,
                    Lifetime = descriptor.Lifetime,
                    DomainObjects = domainObjects,
                    Methods = contract.GetMethods().Select(method => method.Name).Distinct().OrderBy(name => name).ToList(),
                    Controllers = relatedControllers
                };
            }).ToList();

            return new BusinessObjectContextSnapshot
            {
                DomainObjects = domainTypes,
                Services = services,
                ControllerRoutes = apiSurfaces.SelectMany(surface => surface.Routes).Distinct().OrderBy(route => route).ToList()
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method BusinessObjectContextService.CreateSnapshot failed: {__serviceMethodException}");
        throw;
    }
}

    private string FriendlyName(Type type)
    {
    try
    {
            if (!type.IsGenericType) return type.Name;
            var baseName = type.Name.Split('`')[0];
            return $"{baseName}<{string.Join(", ", type.GetGenericArguments().Select(FriendlyName))}>";
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method BusinessObjectContextService.FriendlyName failed: {__serviceMethodException}");
        throw;
    }
}

    private IEnumerable<Type> UnwrapTypes(Type type)
    {
        if (type == typeof(void)) yield break;
        if (type.IsArray)
        {
            foreach (var nested in UnwrapTypes(type.GetElementType()!)) yield return nested;
            yield break;
        }
        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
                foreach (var nested in UnwrapTypes(argument)) yield return nested;
        }
        yield return type;
    }
}

/// <summary>
/// Represents a service architecture descriptor.
/// </summary>
public sealed record ServiceArchitectureDescriptor
{
    /// <summary>
    /// Runs the service architecture descriptor operation.
    /// </summary>
    public ServiceArchitectureDescriptor(Type? interfaceType, Type implementationType, string lifetime)
    {
        InterfaceType = interfaceType;
        ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
        Lifetime = string.IsNullOrWhiteSpace(lifetime) ? "Unknown" : lifetime;
    }

    /// <summary>
    /// Gets interface type.
    /// </summary>
    public Type? InterfaceType { get; }
    /// <summary>
    /// Gets implementation type.
    /// </summary>
    public Type ImplementationType { get; }
    /// <summary>
    /// Gets lifetime.
    /// </summary>
    public string Lifetime { get; }
}

/// <summary>
/// Defines the service architecture registry contract.
/// </summary>
public interface IServiceArchitectureRegistry
{
    IReadOnlyList<ServiceArchitectureDescriptor> Descriptors { get; }
}

/// <summary>
/// Provides service architecture registry operations.
/// </summary>
public sealed class ServiceArchitectureRegistry(IEnumerable<ServiceArchitectureDescriptor> descriptors) : IServiceArchitectureRegistry
{
    /// <summary>
    /// Gets descriptors.
    /// </summary>
    public IReadOnlyList<ServiceArchitectureDescriptor> Descriptors { get; } = descriptors
        .Where(descriptor => descriptor is not null)
        .DistinctBy(descriptor => (descriptor.InterfaceType, descriptor.ImplementationType, descriptor.Lifetime))
        .OrderBy(descriptor => descriptor.ImplementationType.FullName ?? descriptor.ImplementationType.Name, StringComparer.Ordinal)
        .ToList()
        .AsReadOnly();
}
