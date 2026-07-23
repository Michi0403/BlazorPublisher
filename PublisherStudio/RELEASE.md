# PublisherStudio v1.0.59 release

See `CHANGELOG-v1.0.59.md`, `CHANGELOG-v1.0.58.md`, and `VALIDATION.md`.

The publication canvas now uses Chromium CSS layout zoom where supported, with the previous transform scaler retained as a fallback. This keeps high-zoom editor text and embedded browser controls sharper on UHD displays without changing PDF, image, SVG, website, standalone HTML, or print rendering.

Provider Map is no longer silently created as a keyless Google map. Component Studio requires an explicitly selected supported provider and API key, the runtime renders a local placeholder and performs no provider request when that configuration is absent, and Google Map ID configuration is exposed for advanced markers. The bundled Vector Map is the keyless/offline alternative. Map/Vector Map viewport persistence now waits for a real user gesture to finish, avoids zoom-control rerender feedback, and retains Vector Map’s native zoom-factor range instead of clamping it to provider-map limits.

Chart Studio now exposes argument typing, repeated-category aggregation, point ordering, automatic role mapping, numeric-measure guidance, and the existing specialized range, bubble, financial, Sankey, and TreeMap roles. Browser preview and export use the same typed/aggregated chart runtime. Creative editor dialogs expand to the available viewport with an equal responsive shadow gap.

Application and installer version `1.0.59`; publication format `1.46`; picture format `1.2`. There is no separate Media Host executable or release payload.
