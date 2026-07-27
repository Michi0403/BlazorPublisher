using PublisherStudio.Controllers;
using PublisherStudio.Diagnostics;
using PublisherStudio.Domain;
using PublisherStudio.HostedServices.Streaming;
using PublisherStudio.HostedServices.OrganicPlugins;
using PublisherStudio.Services.Automation;
using PublisherStudio.Services.CodeEditing;
using PublisherStudio.Services.Configuration;
using PublisherStudio.Services.MediaConversion;
using PublisherStudio.Services.MediaStudio.UseCases;
using PublisherStudio.Services.OpenScad;
using PublisherStudio.Services.OrganicPlugins;
using PublisherStudio.Services.Panels;
using PublisherStudio.Services.PictureStudio.Import;
using PublisherStudio.Services.Publication;
using PublisherStudio.Services.UserExperience;
using PublisherStudio.Services.Publication.Import;
using PublisherStudio.Services.VideoStudio.Export;
using PublisherStudio.Services.VideoStudio.Import;

namespace PublisherStudio.Services;

public static class PublisherStudioServiceCollectionExtensions
{
    public static IServiceCollection AddPublisherStudioApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PublisherStudioPathOptions>(configuration.GetSection("PublisherStudio:Paths"));
        services.Configure<OrganicPluginOptions>(configuration.GetSection(OrganicPluginOptions.SectionName));

        AddSingleton<IApplicationPortResolver, ApplicationPortResolver>(services);
        AddSingleton<IRuntimeEndpointWriter, RuntimeEndpointWriter>(services);
        AddSingleton<SystemFontCatalog, SystemFontCatalog>(services);
        AddSingleton<PictureDocumentService, PictureDocumentService>(services);
        AddSingleton<OpenRasterImportService, OpenRasterImportService>(services);
        AddSingleton<OpenDocumentImportService, OpenDocumentImportService>(services);
        AddSingleton<MediaTimelineEditService, MediaTimelineEditService>(services);
        AddSingleton<VideoProjectImportService, VideoProjectImportService>(services);
        AddSingleton<SpreadsheetDocumentService, SpreadsheetDocumentService>(services);
        AddSingleton<SpreadsheetSessionStore, SpreadsheetSessionStore>(services);
        AddSingleton<PublicationDataService, PublicationDataService>(services);
        AddSingleton<PublicationComponentService, PublicationComponentService>(services);
        AddSingleton<PanelDocumentService, PanelDocumentService>(services);
        AddSingleton<IMediaConversionService, MediaConversionService>(services);
        AddSingleton<PublicationWebhookStore, PublicationWebhookStore>(services);
        AddSingleton<PublicationLiveDataRegistry, PublicationLiveDataRegistry>(services);
        AddSingleton<PublicationWebDataService, PublicationWebDataService>(services);
        AddSingleton<PublicationFileService, PublicationFileService>(services);
        AddSingleton<PublicationMediaAssetStore, PublicationMediaAssetStore>(services);
        AddSingleton<PublicationRecoveryService, PublicationRecoveryService>(services);
        AddSingleton<StreamingProfileStore, StreamingProfileStore>(services);
        AddSingleton<PublicationStreamingSettingsStore, PublicationStreamingSettingsStore>(services);
        AddSingleton<TwitchOAuthService, TwitchOAuthService>(services);
        services.AddHostedService<TwitchOAuthMaintenanceService>();
        services.AddPublisherStreaming();
        AddSingleton<StreamingMediaHostClient, StreamingMediaHostClient>(services);
        AddSingleton<StreamingSessionService, StreamingSessionService>(services);

        AddSingleton<IPolygonGeometryService, PolygonGeometryService>(services);
        AddSingleton<IBrowserRuntimeTemplateService, BrowserRuntimeTemplateService>(services);
        AddSingleton<IOpenScadCatalogService, OpenScadCatalogService>(services);
        AddSingleton<IOpenScadValueFormatter, OpenScadValueFormatter>(services);
        AddSingleton<IOpenScadNodeFactoryService, OpenScadNodeFactoryService>(services);
        AddSingleton<IOpenScadNodeRenderer, OpenScadPrimitiveNodeRenderer>(services);
        AddSingleton<IOpenScadNodeRenderer, OpenScadWrapperNodeRenderer>(services);
        AddSingleton<IOpenScadNodeRenderer, OpenScadRawNodeRenderer>(services);
        AddSingleton<IOpenScadNodeRenderer, OpenScadModuleCallNodeRenderer>(services);
        AddSingleton<IOpenScadDocumentService, OpenScadDocumentService>(services);
        AddSingleton<IOpenScadVideoLayerAdapter, OpenScadVideoLayerAdapter>(services);
        services.AddSingleton<VideoLayerInterchangeService>();
        services.AddSingleton<IVideoLayerInterchangeService>(provider => provider.GetRequiredService<VideoLayerInterchangeService>());
        services.AddSingleton(new ServiceArchitectureDescriptor(typeof(IVideoLayerInterchangeService), typeof(VideoLayerInterchangeService), "Singleton"));

        AddSingleton<IUserInputAutomationService, UserInputAutomationService>(services);
        AddSingleton<IScreenshotCaptureService, ScreenshotCaptureService>(services);
        AddSingleton<IApplicationPathService, ApplicationPathService>(services);
        AddSingleton<IFileLocalizationService, FileLocalizationService>(services);
        AddSingleton<IRenderExportCatalogService, RenderExportCatalogService>(services);
        AddSingleton<ICodeLanguageService, CodeLanguageService>(services);
        AddSingleton<ICodeFormattingService, CodeFormattingService>(services);
        AddSingleton<IPublicationElementLayoutService, PublicationElementLayoutService>(services);
        AddScoped<IUserNotificationService, UserNotificationService>(services);

        AddSingleton<IApiSurfaceCatalogService, ApiSurfaceCatalogService>(services);
        services.AddSingleton<IServiceArchitectureRegistry, ServiceArchitectureRegistry>();
        AddSingleton<IBusinessObjectContextService, BusinessObjectContextService>(services);

        AddSingleton<IOrganicPluginProtocolCodec, OrganicPluginProtocolCodec>(services);
        AddSingleton<IOrganicRuntimeSecurityService, OrganicRuntimeSecurityService>(services);
        AddSingleton<ILocalGptDiscoveryRegistry, LocalGptDiscoveryRegistry>(services);
        AddSingleton<IOrganicCapabilityCatalog, OrganicCapabilityCatalog>(services);
        AddSingleton<IOrganicPermissionStore, OrganicPermissionStore>(services);
        AddSingleton<IOrganicResultStore, OrganicResultStore>(services);
        AddSingleton<IOrganicWorkExecutor, OrganicWorkExecutor>(services);
        AddSingleton<IOrganicWorkCoordinator, OrganicWorkCoordinator>(services);
        AddSingleton<ILocalGptConnectionService, LocalGptConnectionService>(services);
        AddSingleton<IRecurringScreenReaderService, RecurringScreenReaderService>(services);

        services.AddHostedService<LocalGptDiscoveryHostedService>();

        AddScoped<EditorStateService, EditorStateService>(services);
        AddScoped<PictureEditorStateService, PictureEditorStateService>(services);
        AddArchitectureDescriptors(services);
        services.AddHostedService<ServiceRegistrationLoggingHostedService>();
        return services;
    }

    private static void AddArchitectureDescriptors(IServiceCollection services)
    {
        var existing = services
            .Where(descriptor => descriptor.ServiceType == typeof(ServiceArchitectureDescriptor))
            .Select(descriptor => descriptor.ImplementationInstance as ServiceArchitectureDescriptor)
            .Where(descriptor => descriptor is not null)
            .Cast<ServiceArchitectureDescriptor>()
            .Select(descriptor => (descriptor.InterfaceType, descriptor.ImplementationType, descriptor.Lifetime))
            .ToHashSet();

        var candidates = services.ToList().Select(descriptor =>
        {
            var implementation = descriptor.ImplementationType ?? descriptor.ImplementationInstance?.GetType();
            if (implementation is null && descriptor.ServiceType.IsClass)
                implementation = descriptor.ServiceType;

            // Factory registrations expose neither ImplementationType nor ImplementationInstance.
            // They remain valid DI registrations, but cannot be reflected as an implementation
            // architecture descriptor until a concrete implementation type is known.
            if (implementation is null)
                return null;

            var applicationOwned = implementation.Namespace?.StartsWith("PublisherStudio", StringComparison.Ordinal) == true
                || descriptor.ServiceType.Namespace?.StartsWith("PublisherStudio", StringComparison.Ordinal) == true;
            if (!applicationOwned || implementation == typeof(ServiceArchitectureDescriptor)) return null;
            var contract = descriptor.ServiceType.IsInterface ? descriptor.ServiceType : null;
            return new ServiceArchitectureDescriptor(contract, implementation, descriptor.Lifetime.ToString());
        }).Where(descriptor => descriptor is not null).Cast<ServiceArchitectureDescriptor>()
          .DistinctBy(descriptor => (descriptor.InterfaceType, descriptor.ImplementationType, descriptor.Lifetime))
          .ToList();

        foreach (var descriptor in candidates)
        {
            var key = (descriptor.InterfaceType, descriptor.ImplementationType, descriptor.Lifetime);
            if (existing.Add(key)) services.AddSingleton(descriptor);
        }
    }

    private static void AddSingleton<TContract, TImplementation>(IServiceCollection services)
        where TContract : class
        where TImplementation : class, TContract
    {
        services.AddSingleton<TContract, TImplementation>();
        services.AddSingleton(new ServiceArchitectureDescriptor(typeof(TContract), typeof(TImplementation), "Singleton"));
    }

    private static void AddScoped<TContract, TImplementation>(IServiceCollection services)
        where TContract : class
        where TImplementation : class, TContract
    {
        services.AddScoped<TContract, TImplementation>();
        services.AddSingleton(new ServiceArchitectureDescriptor(typeof(TContract), typeof(TImplementation), "Scoped"));
    }
}
