using System.Reflection;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Automation;

/// <summary>
/// Coordinates business object context behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="architecture">Service architecture registry dependency used by the business object context workflow to provide the corresponding application capability.</param>
/// <param name="apiSurfaceCatalog">Api surface catalog service dependency used by the business object context workflow to provide the corresponding application capability.</param>
public sealed class BusinessObjectContextService(
    IServiceArchitectureRegistry architecture,
    IApiSurfaceCatalogService apiSurfaceCatalog) : IBusinessObjectContextService
{
    /// <summary>
    /// Stores the internal assembly state used by <see cref="BusinessObjectContextService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Assembly _assembly = typeof(BusinessObjectContextService).Assembly;

    /// <summary>
    /// Creates snapshot as part of the business object context service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The business object context snapshot produced by the operation.</returns>
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

    /// <summary>
    /// Performs friendly name as part of the business object context service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="type">Type value supplied to the business object context operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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

    /// <summary>
    /// Performs unwrap types as part of the business object context service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="type">Type value supplied to the business object context operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IEnumerable<Type> UnwrapTypes(Type type)
    {
        System.Diagnostics.Trace.TraceInformation($"Entering iterator BusinessObjectContextService.UnwrapTypes for {type.FullName ?? type.Name}.");
        try
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
        finally
        {
            System.Diagnostics.Trace.TraceInformation($"Completed iterator BusinessObjectContextService.UnwrapTypes for {type.FullName ?? type.Name}.");
        }
    }
}

/// <summary>
/// Represents service architecture state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
public sealed record ServiceArchitectureDescriptor
{
    /// <summary>
    /// Initializes a new <see cref="ServiceArchitectureDescriptor"/> instance and captures the dependencies or initial state required by its service architecture workflow.
    /// </summary>
    /// <param name="interfaceType">Interface type value supplied to the service architecture operation and used when producing its result.</param>
    /// <param name="implementationType">Implementation type value supplied to the service architecture operation and used when producing its result.</param>
    /// <param name="lifetime">Lifetime value supplied to the service architecture operation and used when producing its result.</param>
    public ServiceArchitectureDescriptor(Type? interfaceType, Type implementationType, string lifetime)
    {
        InterfaceType = interfaceType;
        ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
        Lifetime = string.IsNullOrWhiteSpace(lifetime) ? "Unknown" : lifetime;
    }

    /// <summary>
    /// Gets the interface type value that forms part of the service architecture state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The interface type value exposed by <see cref="ServiceArchitectureDescriptor"/>.</value>
    public Type? InterfaceType { get; }
    /// <summary>
    /// Gets the implementation type value that forms part of the service architecture state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The implementation type value exposed by <see cref="ServiceArchitectureDescriptor"/>.</value>
    public Type ImplementationType { get; }
    /// <summary>
    /// Gets the lifetime value that forms part of the service architecture state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The lifetime value exposed by <see cref="ServiceArchitectureDescriptor"/>.</value>
    public string Lifetime { get; }
}

/// <summary>
/// Defines the contract for service architecture behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IServiceArchitectureRegistry
{
    /// <summary>
    /// Gets the descriptors collection maintained or exposed by this service architecture instance for downstream processing.
    /// </summary>
    /// <value>The descriptors value exposed by <see cref="IServiceArchitectureRegistry"/>.</value>
    IReadOnlyList<ServiceArchitectureDescriptor> Descriptors { get; }
}

/// <summary>
/// Maintains the authoritative directory of service architecture entries used for discovery, validation, and runtime lookup.
/// </summary>
/// <param name="descriptors">Service architecture descriptor dependency used by the service architecture workflow to provide the corresponding application capability.</param>
public sealed class ServiceArchitectureRegistry(IEnumerable<ServiceArchitectureDescriptor> descriptors) : IServiceArchitectureRegistry
{
    /// <summary>
    /// Gets the descriptors collection maintained or exposed by this service architecture instance for downstream processing.
    /// </summary>
    /// <value>The descriptors value exposed by <see cref="ServiceArchitectureRegistry"/>.</value>
    public IReadOnlyList<ServiceArchitectureDescriptor> Descriptors { get; } = descriptors
        .Where(descriptor => descriptor is not null)
        .DistinctBy(descriptor => (descriptor.InterfaceType, descriptor.ImplementationType, descriptor.Lifetime))
        .OrderBy(descriptor => descriptor.ImplementationType.FullName ?? descriptor.ImplementationType.Name, StringComparer.Ordinal)
        .ToList()
        .AsReadOnly();
}
