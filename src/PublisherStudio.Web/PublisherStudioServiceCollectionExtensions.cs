using PublisherStudio.Controllers;
using PublisherStudio.Diagnostics;
using PublisherStudio.BusinessObjects;
using PublisherStudio.BusinessObjects.Diagnostics;
using PublisherStudio.HostedServices.Streaming;
using PublisherStudio.HostedServices.OrganicPlugins;
using PublisherStudio.Services.Automation;
using PublisherStudio.Services.CodeEditing;
using PublisherStudio.Services.Configuration;
using PublisherStudio.Services.Documentation;
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
using PublisherStudio.Services.Streaming.Capture;
using PublisherStudio.Services.Streaming.Encoding;
using PublisherStudio.Services.Streaming.Metadata;

namespace PublisherStudio.Services;

/// <summary>
/// Represents a PublisherStudio service collection extensions application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public static class PublisherStudioServiceCollectionExtensions
{
    /// <summary>
    /// Adds PublisherStudio application for <see cref="PublisherStudioServiceCollectionExtensions"/>, keeping the operation consistent with the state and invariants of the surrounding PublisherStudio service collection extensions workflow.
    /// </summary>
    /// <param name="services">Service collection dependency used by the PublisherStudio service collection extensions workflow to provide the corresponding application capability.</param>
    /// <param name="configuration">Configuration containing the caller-supplied values that control this operation.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <returns>The i service collection produced by the operation.</returns>
    public static IServiceCollection AddPublisherStudioApplication(this IServiceCollection services, IConfiguration configuration, ILogger logger)
    {
        try
        {
        var configurationDocument = configuration.Get<PublisherStudioConfigurationDocument>()
            ?? throw new InvalidDataException("PublisherStudio configuration could not be loaded.");
        services.AddSingleton(configurationDocument.Twitch);
        services.AddSingleton(configurationDocument.OrganicPlugins);
        services.AddSingleton(configurationDocument.PublisherStudio);
        services.AddSingleton(configurationDocument.PublisherStudio.RuntimePolicy);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(configurationDocument.PublisherStudio.Paths));
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(configurationDocument.OrganicPlugins));
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(configurationDocument.PublisherStudio.RuntimeValueStores.PanelTextPatterns));
        AddSingleton<IPublisherRuntimePatternService, PublisherRuntimePatternService>(services);

        AddSingleton<IPanelStudioTextPatternDataService, PanelStudioTextPatternDataService>(services);
        AddSingleton<IPublisherRuntimePolicyDataService, PublisherRuntimePolicyDataService>(services);
        AddSingleton<PanelStudioTextService, PanelStudioTextService>(services);
        AddSingleton<IApplicationPortResolver, ApplicationPortResolver>(services);
        AddSingleton<IRuntimeEndpointState, RuntimeEndpointState>(services);
        AddSingleton<IRuntimeEndpointWriter, RuntimeEndpointWriter>(services);
        AddSingleton<SystemFontCatalog, SystemFontCatalog>(services);
        AddSingleton<IPublisherDocumentFactory, PublisherDocumentFactory>(services);
        AddSingleton<IPagePresetCatalog, PagePresetCatalog>(services);
        AddSingleton<IStoryPageLayoutService, StoryPageLayoutService>(services);
        AddSingleton<IPublicationGridRowFactory, PublicationGridRowFactory>(services);
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
        AddSingleton<IPublicationMarkupService, PublicationMarkupService>(services);
        AddSingleton<PublicationFileService, PublicationFileService>(services);
        AddSingleton<PublicationMediaAssetStore, PublicationMediaAssetStore>(services);
        AddSingleton<PublicationRecoveryService, PublicationRecoveryService>(services);
        AddSingleton<StreamingProfileStore, StreamingProfileStore>(services);
        AddSingleton<PublicationStreamingSettingsStore, PublicationStreamingSettingsStore>(services);
        AddSingleton<TwitchOAuthService, TwitchOAuthService>(services);
        services.AddHostedService<TwitchOAuthMaintenanceService>();
        services.AddPublisherStreaming(logger);
        AddSingleton<StreamingMediaHostClient, StreamingMediaHostClient>(services);
        AddSingleton<StreamingSessionService, StreamingSessionService>(services);

        AddSingleton<WordArtPathGeometry, WordArtPathGeometry>(services);
        AddSingleton<ConnectorGeometry, ConnectorGeometry>(services);
        AddSingleton<PublicationAnimationData, PublicationAnimationData>(services);
        AddSingleton<PublicationElementTraversal, PublicationElementTraversal>(services);
        AddSingleton<PublicationMediaData, PublicationMediaData>(services);
        AddSingleton<RichTextDocumentFactory, RichTextDocumentFactory>(services);
        AddSingleton<SvgInterchangeSanitizer, SvgInterchangeSanitizer>(services);
        AddSingleton<NowPlayingReader, NowPlayingReader>(services);
        AddSingleton<FfmpegLocator, FfmpegLocator>(services);
        AddSingleton<NativeDeviceDiscovery, NativeDeviceDiscovery>(services);
        AddSingleton<FfmpegEncoderResolver, FfmpegEncoderResolver>(services);
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
        AddSingleton<IPublisherDocumentationCatalogService, PublisherDocumentationCatalogService>(services);
        AddScoped<IPublisherDocumentationViewerService, PublisherDocumentationViewerService>(services);
        AddSingleton<IRenderExportCatalogService, RenderExportCatalogService>(services);
        AddSingleton<ICodeLanguageService, CodeLanguageService>(services);
        AddSingleton<ICodeFormattingService, CodeFormattingService>(services);
        AddSingleton<IPublicationElementLayoutService, PublicationElementLayoutService>(services);
        AddScoped<IUserNotificationService, UserNotificationService>(services);

        AddSingleton<IApiSurfaceCatalogService, ApiSurfaceCatalogService>(services);
        services.AddSingleton<IServiceArchitectureRegistry, ServiceArchitectureRegistry>();
        AddSingleton<IBusinessObjectContextService, BusinessObjectContextService>(services);

        AddSingleton<IOrganicPluginProtocolCodec, OrganicPluginProtocolCodec>(services);
        AddSingleton<IOrganicTransportSecurityPolicy, OrganicTransportSecurityPolicy>(services);
        AddSingleton<IOrganicConnectionRuntimeState, OrganicConnectionRuntimeStateService>(services);
        AddSingleton<IOrganicWireEnvelopeFactory, OrganicWireEnvelopeFactory>(services);
        AddSingleton<IOrganicRuntimeSecurityService, OrganicRuntimeSecurityService>(services);
        AddSingleton<ILocalGptDiscoveryRegistry, LocalGptDiscoveryRegistry>(services);
        AddSingleton<IPublisherDxFunctionCatalogDataService, PublisherDxFunctionCatalogDataService>(services);
        AddSingleton<OrganicCapabilityCatalog, OrganicCapabilityCatalog>(services);
        AddSingleton<IOrganicCapabilityCatalog, ObjectStoreOrganicCapabilityCatalog>(services);
        services.AddSingleton<LocalGPT.WireProtocol.IOneWireCapabilityProvider>(provider =>
            provider.GetRequiredService<IOrganicCapabilityCatalog>());
        AddSingleton<IOrganicPermissionStore, OrganicPermissionStore>(services);
        AddSingleton<IOrganicResultStore, OrganicResultStore>(services);
        AddSingleton<IOrganicReplayPolicyDataService, OrganicReplayPolicyDataService>(services);
        AddSingleton<IOrganicReplayGuard, OrganicReplayGuard>(services);
        AddSingleton<IOrganicWorkExecutor, OrganicWorkExecutor>(services);
        AddSingleton<IOrganicWorkCoordinator, OrganicWorkCoordinator>(services);
        AddSingleton<ILocalGptConnectionService, LocalGptConnectionService>(services);
        AddSingleton<IRecurringScreenReaderService, RecurringScreenReaderService>(services);

        services.AddHostedService<LocalGptDiscoveryHostedService>();

        AddScoped<EditorStateService, EditorStateService>(services);
        AddScoped<PictureEditorStateService, PictureEditorStateService>(services);
        AddArchitectureDescriptors(services);
        services.Configure<DebugExceptionDiagnosticsOptions>(configuration.GetSection("PublisherStudio:DebugExceptionDiagnostics"));
        services.AddHostedService<DebugFirstChanceExceptionLoggingHostedService>();
        services.AddHostedService<ServiceRegistrationLoggingHostedService>();
        logger.LogInformation($"Registered PublisherStudio application services and architecture descriptors.");
        return services;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublisherStudio service registration failed: {exception.Message}");
            throw;
        }

        void AddArchitectureDescriptors(IServiceCollection targetServices)
        {
            try
            {
        var existing = services
            .Where(descriptor => descriptor.ServiceType == typeof(ServiceArchitectureDescriptor))
            .Select(descriptor => descriptor.ImplementationInstance as ServiceArchitectureDescriptor)
            .Where(descriptor => descriptor is not null)
            .Cast<ServiceArchitectureDescriptor>()
            .Select(descriptor => (descriptor.InterfaceType, descriptor.ImplementationType, descriptor.Lifetime))
            .ToHashSet();

        var candidates = targetServices.ToList().Select(descriptor =>
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
            if (existing.Add(key)) targetServices.AddSingleton(descriptor);
        }
                logger.LogTrace($"Registered {candidates.Count} PublisherStudio architecture descriptor candidates.");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"PublisherStudio architecture descriptor registration failed: {exception.Message}");
                throw;
            }
        }

        void AddSingleton<TContract, TImplementation>(IServiceCollection targetServices)
            where TContract : class
            where TImplementation : class, TContract
        {
            try
            {
                targetServices.AddSingleton<TContract, TImplementation>();
                targetServices.AddSingleton(new ServiceArchitectureDescriptor(typeof(TContract), typeof(TImplementation), "Singleton"));
                logger.LogTrace($"Registered singleton service {typeof(TContract).FullName} with implementation {typeof(TImplementation).FullName}.");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"Singleton service registration failed for {typeof(TContract).FullName}: {exception.Message}");
                throw;
            }
        }

        void AddScoped<TContract, TImplementation>(IServiceCollection targetServices)
            where TContract : class
            where TImplementation : class, TContract
        {
            try
            {
                targetServices.AddScoped<TContract, TImplementation>();
                targetServices.AddSingleton(new ServiceArchitectureDescriptor(typeof(TContract), typeof(TImplementation), "Scoped"));
                logger.LogTrace($"Registered scoped service {typeof(TContract).FullName} with implementation {typeof(TImplementation).FullName}.");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"Scoped service registration failed for {typeof(TContract).FullName}: {exception.Message}");
                throw;
            }
        }
    }
}
