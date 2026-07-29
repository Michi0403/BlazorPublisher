# PublisherStudio safe rework manifest

This manifest compares the delivered tree with the latest user-owned base used for this pass.

## Design constraints applied

- `Program.cs` remains a legal bootstrap/static boundary.
- Framework-required Blazor imports remain unchanged.
- PublisherStudio DI extension methods remain static extensions and accept `ILogger` with `try/catch` logging.
- No namespace declaration was renamed.
- No `.pubxml`, project file, installer project, or release lane was removed.
- Runtime regex source, collections, limits, identifiers, and security values are owned by typed/persisted policy services rather than static catalogs.
- DTOs, records, constructors, and pure calculations are not wrapped in artificial logging/exception boilerplate.
- Static, runtime-value, and maintained operational-method debt baselines are empty and unused.

Added files: 9
Removed files: 0
Changed files: 78

## Added

- `build/Invoke-ArchitectureAudit.ps1`
- `build/audit_application_architecture.py`
- `build/tests/test_architecture_audit.py`
- `docs/SAFE_STATIC_RUNTIME_AND_DIAGNOSTICS_POLICY.md`
- `src/PublisherStudio.Web/Services/Configuration/IPublisherRuntimePatternService.cs`
- `src/PublisherStudio.Web/Services/Configuration/PublisherRuntimePatternService.cs`
- `src/PublisherStudio.Web/Services/PublicationMarkupService.cs`
- `src/PublisherStudio.Web/Services/PublisherDocumentFactory.cs`
- `tests/applicationArchitecturePolicy.test.mjs`

## Removed


## Changed

- `build/Assert-ApplicationStaticPolicy.ps1`
- `build/Assert-MethodDiagnostics.ps1`
- `build/Assert-RuntimeValueOwnership.ps1`
- `build/application-static-baseline.json`
- `build/method-diagnostics-baseline.json`
- `build/runtime-value-ownership-baseline.json`
- `src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor`
- `src/PublisherStudio.Web/Components/Editor/MediaStudio.razor`
- `src/PublisherStudio.Web/Components/Editor/PageNavigator.razor`
- `src/PublisherStudio.Web/Components/Editor/PageSurface.razor`
- `src/PublisherStudio.Web/Components/Editor/PanelStudio.razor`
- `src/PublisherStudio.Web/Components/Editor/PanelView.razor`
- `src/PublisherStudio.Web/Components/Editor/PictureEditor.razor.cs`
- `src/PublisherStudio.Web/Components/Editor/PrintPublication.razor`
- `src/PublisherStudio.Web/Components/Editor/PublicationRibbon.razor`
- `src/PublisherStudio.Web/Components/Editor/StoryEditor.razor`
- `src/PublisherStudio.Web/Components/Editor/StreamingStudio.razor`
- `src/PublisherStudio.Web/Components/Editor/WordArtPathEditor.razor`
- `src/PublisherStudio.Web/Components/Editor/WordArtView.razor`
- `src/PublisherStudio.Web/Components/Pages/Editor.razor`
- `src/PublisherStudio.Web/Components/_Imports.razor`
- `src/PublisherStudio.Web/Controllers/PublicationController.cs`
- `src/PublisherStudio.Web/Domain/ConnectorGeometry.cs`
- `src/PublisherStudio.Web/Domain/PagePresets.cs`
- `src/PublisherStudio.Web/Domain/PictureStudioModels.cs`
- `src/PublisherStudio.Web/Domain/PublicationAnimationData.cs`
- `src/PublisherStudio.Web/Domain/PublicationDataModels.cs`
- `src/PublisherStudio.Web/Domain/PublicationElementTraversal.cs`
- `src/PublisherStudio.Web/Domain/PublicationMediaModels.cs`
- `src/PublisherStudio.Web/Domain/PublicationModels.cs`
- `src/PublisherStudio.Web/Domain/PublisherRuntimePolicyModels.cs`
- `src/PublisherStudio.Web/Domain/WordArtPathGeometry.cs`
- `src/PublisherStudio.Web/PublisherStudioServiceCollectionExtensions.cs`
- `src/PublisherStudio.Web/Services/ApplicationHostServices.cs`
- `src/PublisherStudio.Web/Services/CodeEditing/CodeFormattingService.cs`
- `src/PublisherStudio.Web/Services/Configuration/ApplicationPathService.cs`
- `src/PublisherStudio.Web/Services/Configuration/FileLocalizationService.cs`
- `src/PublisherStudio.Web/Services/Configuration/IPublisherRuntimePolicyDataService.cs`
- `src/PublisherStudio.Web/Services/Configuration/PanelTextPatternStoreOptions.cs`
- `src/PublisherStudio.Web/Services/Configuration/PublisherRuntimePolicyDataService.cs`
- `src/PublisherStudio.Web/Services/Configuration/RenderExportCatalogService.cs`
- `src/PublisherStudio.Web/Services/EditorStateService.cs`
- `src/PublisherStudio.Web/Services/MediaConversion/MediaConversionService.cs`
- `src/PublisherStudio.Web/Services/MediaStudio/UseCases/MediaTimelineEditService.cs`
- `src/PublisherStudio.Web/Services/OrganicPlugins/LocalGptConnectionService.cs`
- `src/PublisherStudio.Web/Services/PictureStudio/Import/OpenRasterImportService.cs`
- `src/PublisherStudio.Web/Services/PictureStudio/Import/SvgInterchangeSanitizer.cs`
- `src/PublisherStudio.Web/Services/Publication/Import/OpenDocumentImportService.cs`
- `src/PublisherStudio.Web/Services/PublicationComponentService.cs`
- `src/PublisherStudio.Web/Services/PublicationDataService.cs`
- `src/PublisherStudio.Web/Services/PublicationFileService.cs`
- `src/PublisherStudio.Web/Services/PublicationMediaAssetStore.cs`
- `src/PublisherStudio.Web/Services/PublicationWebDataService.cs`
- `src/PublisherStudio.Web/Services/SpreadsheetDocumentService.cs`
- `src/PublisherStudio.Web/Services/Streaming/Capture/NativeCaptureRegistry.cs`
- `src/PublisherStudio.Web/Services/Streaming/Capture/NativeDeviceDiscovery.cs`
- `src/PublisherStudio.Web/Services/Streaming/Encoding/EncoderOrchestrator.cs`
- `src/PublisherStudio.Web/Services/Streaming/Encoding/FfmpegLocator.cs`
- `src/PublisherStudio.Web/Services/Streaming/Metadata/NowPlayingReader.cs`
- `src/PublisherStudio.Web/Services/Streaming/Sessions/MediaSession.cs`
- `src/PublisherStudio.Web/Services/Streaming/Sessions/MediaSessionRegistry.cs`
- `src/PublisherStudio.Web/Services/Streaming/UseCases/Chat/StreamingChatUseCases.cs`
- `src/PublisherStudio.Web/Services/Streaming/UseCases/Runtime/StreamingRuntimeUseCases.cs`
- `src/PublisherStudio.Web/Services/SystemFontCatalog.cs`
- `src/PublisherStudio.Web/Services/VideoStudio/Import/VideoProjectImportService.cs`
- `src/PublisherStudio.Web/StreamingServiceCollectionExtensions.cs`
- `src/PublisherStudio.Web/appsettings.json`
- `src/PublisherStudio.Web/package.json`
- `tests/architectureContract.test.mjs`
- `tests/canvasZoomScaling.test.mjs`
- `tests/final28PublishConfigurationSync.test.mjs`
- `tests/localizationAndCodeEditing.test.mjs`
- `tests/openInterchange.test.mjs`
- `tests/panelAndMediaConversion.test.mjs`
- `tests/panelComposerMediaWorkflow.test.mjs`
- `tests/signalRuntime.test.mjs`
- `tests/streamingRuntime.test.mjs`
- `tests/videoProjectImport.test.mjs`

## Validation performed in this environment

- Python architecture audit: passed.
- Python architecture-audit unit tests: passed.
- Application-static scan: only `Program.cs`, the Blazor RenderMode import, and the two PublisherStudio DI extension boundaries remain.
- Namespace comparison against the base: no namespace declaration changed.
- Complete PublisherStudio `npm test` suite: passed, including the new architecture-policy contract.

A .NET compile/publish was not possible in this environment; compiler confirmation remains a local merge gate.
