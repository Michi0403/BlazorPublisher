# PublisherStudio 4.4.6 — AI Chat localization and XML documentation build repair

- Add the exact `Insert DevExtreme Chat with optional LocalGPT or server-configured OpenAI` tooltip key to all six PublisherStudio localization catalogs, with reviewed distinct translations (including German). Retain `LT` localization call and zero-exemption build guard.
- Repair XML documentation of the `PublisherAiController` constructor's `directAi` parameter (CS1573 warning).
- Remove stale duplicated `parameters` documentation before `OrganicWorkExecutor.QueueBrowserView` (CS1571 warning), preserving the operation and permissions.
- Update Web and InstallerConsole version metadata to 4.4.6.
- Existing source-architecture checks and DevExtreme/LocalGPT provider behaviors are unchanged.
