# PublisherStudio 2.6.3 — Preview presets, LocalGPT AI components, website compression, and editor UX

## Panel Studio preview and inspector UX

- Panel Studio preview viewports are no longer a closed built-in list. The existing phone/tablet/laptop/wide presets remain available and users can create, update, select, and delete their own named width/height presets.
- Custom viewport presets are persisted through the existing PublisherStudio system-variable store under `PanelStudio.PreviewViewportPresets` instead of introducing a second settings store.
- Preview viewport simulation remains isolated from authored panel geometry: changing the simulated device does not resize the saved panel or its elements.
- Reworked the right-hand inspector into a stable top component list plus an independently scrollable property region so the first list no longer disappears inside the long property scroll.
- Nested scrolling now dismisses/repositions PublisherStudio tooltips rather than leaving a stale hover card offset from the item that originally produced it.
- Panel Studio footer actions now use themed DevExpress buttons instead of the visually disconnected native-button row.

## LocalGPT AI authoring and insertable Chat

- When PublisherStudio is linked to a LocalGPT peer that advertises `council.run`, Mainframe quick insert exposes **AI Text** and **AI Chat**.
- **AI Text** creates a normal PublisherStudio text frame and opens StoryEditor directly into the established LocalGPT proposal workflow. It does not create a special incompatible AI-only text object.
- StoryEditor gains selection-aware AI editing commands for proofread, shorten, expand, summarize, explain, professional tone, friendly tone, and translate.
- Selection editing uses the DevExpress RichEdit active subdocument/selection interval APIs and keeps the result reviewable before the author explicitly replaces the selected text.
- **AI Chat** creates the existing insertable DevExtreme Chat publication component and configures it for LocalGPT Council mode. The existing JavaScript component runtime is reused rather than introducing a parallel browser component stack.
- AI-enabled Chat requests call a same-origin PublisherStudio `/api/publisher-ai/chat` adapter. PublisherStudio owns the paired 1-Wire connection and routes the request to LocalGPT Council; browser components never receive LocalGPT transport secrets or provider credentials.
- Normal Twitch/YouTube/custom/output-context Chat behavior remains unchanged when LocalGPT AI mode is disabled.
- Exported publications remain fully usable without LocalGPT for normal data, visuals, navigation, and component behavior. An explicitly AI-enabled Chat reports AI unavailability gracefully when no PublisherStudio/LocalGPT bridge exists.
- The current PublisherStudio 1-Wire client exposes accepted/approval/final-result envelopes rather than a Council token stream, so this release deliberately uses a visible waiting assistant message followed by the authoritative final Council result instead of fabricating token streaming.
- No fake **AI Image** command was added: the currently paired LocalGPT capability directory does not provide a verified image-generation contract that PublisherStudio can safely invoke yet.

## Single-file and presentation website compression

- **Export single-file website** and **Export presentation website** now open the same class of media-compression choice used by structured website export instead of exporting immediately with no user control.
- Image choices are Preserve, PNG, WebP, and AVIF with quality where appropriate. SVG/animated sources are preserved and unsupported or non-beneficial conversions keep their source.
- Video choices are Preserve or local WebM conversion with quality/fallback controls.
- Both single-file modes reuse the existing structured-site image/video encoding helpers so PublisherStudio has one browser compression implementation rather than divergent exporters.
- Preserve remains the default and source publication media is never mutated by an export optimization choice.

## Language selector

- Moved culture selection out of the floating overlay and into the existing blue publication title/modified-time bar.
- The selector keeps the existing full-page culture reload semantics, but no longer obscures the Mainframe or consumes a separate overlay position.
- The title bar's spacing now adapts to the embedded culture control instead of reserving space for the removed floating selector.

## Publication and component model

- Publication format is now **1.58** for persisted DevExtreme Chat AI configuration.
- Added provider-neutral Chat AI metadata: mode, Council team, optional author/system instructions, memory flags, and max output-token budget.
- PublisherStudio Web and InstallerConsole are **2.6.3**.
- Picture Studio document format remains **1.5**.

## Scope protection

- Existing JavaScript pull/runtime mechanisms are reused for exported DevExtreme components and website output.
- The ordinary publication data/runtime path remains independent of LocalGPT.
- No LocalGPT browser URL, Ollama endpoint, or model/provider credential is embedded into publication objects.
- The word-processing print-fidelity path discussed separately was intentionally not refactored in this release.
- No GitHub access and no dotnet/MSBuild build were used while preparing this source package.
