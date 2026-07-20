# PublisherStudio v1.0.42 — reversible previews and signal resizing

## Preview reset lifecycle

- **Stop preview** now stops both the regular animation preview and the Signal Arrow/Connector runtime.
- Before preview playback, PublisherStudio records the affected object state. Stopping or completing an editor animation preview restores the original transform, opacity, inline CSS, class list, hidden/visibility state, dimensions, and media position.
- Signal actions that intentionally leave a transform, opacity, visibility, CSS class, or media change in place remain persistent in HTML playback, but are reversible from the editor with **Stop preview**.
- Starting a page preview or re-entering an exported page resets the previous signal state before evaluating page-entry triggers. This gives every run a stable object-derived starting condition.

## Signal resizing and trigger monitoring

- Signal travel morphs now support independent **Resize width (%)** and **Resize height (%)** values in addition to transform scaling.
- Resizing uses the target's measured rendered dimensions, so it works for publication objects and inner HTML/component targets.
- Click and hover triggers use delegated runtime monitoring instead of one-time listeners on the original DOM nodes. They continue to work after Blazor, DevExtreme, spreadsheet, chart, or custom HTML content rerenders.
- A `MutationObserver` keeps visible connector hit-testing synchronized when signal settings or connector nodes change.
- Auto-reverse traversal is counted consistently in motion, visible signal playback, and video-export duration calculation.

## Offline runtime

- The embedded single-file signal runtime is self-contained and no longer relies on an editor-only reduced-motion helper.
- Reset, resizing, dynamic trigger monitoring, and page-entry baseline restoration are included in offline presentation and website HTML exports.

## File formats and version

- Application/package/installer version: `1.0.42`.
- Publication document format: `1.40`.
- Picture document format: `1.2`.
