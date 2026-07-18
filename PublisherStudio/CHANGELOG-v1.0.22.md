# PublisherStudio v1.0.22 selection and desktop clipboard stabilization

- Restored publication-canvas mouse selection while retaining the v1.0.21 keyboard clipboard and desktop drag/drop workflow. Canvas pointer handling now runs in capture phase so nested video controls, SVG, charts, and animation content cannot swallow object selection.
- Removed revision-driven canvas reinitialization. Selection changes are synchronized explicitly instead, preventing ordinary document revisions from cancelling or invalidating the active mouse mode.
- Added immediate browser-side selection synchronization for normal, additive, grouped, and connector selection. The Blazor state remains authoritative and reconciles the optimistic outline on render.
- Kept animation preview cleanup compatible with editing: the first pointer press safely ends preview interception and then continues through normal object selection.
- Hardened multi-object movement across z-levels. The server now merges the full current selection and persistent group membership with the browser movement list, so selected or grouped objects underneath other objects cannot be left behind.
- Added desktop clipboard paste for pictures, videos, text files, Markdown files, image/video clipboard blobs, and plain text. External clipboard content is inserted at the last clicked publication position and reuses the unrestricted asset import path used by drag/drop.
- Preserved PublisherStudio's internal multi-object clipboard. Ctrl+V distinguishes the active internal clipboard from content copied in another desktop application, with a fallback to internal paste when the browser exposes no external clipboard payload.
- Added the missing `Microsoft.AspNetCore.Components.Server` import required by the `CircuitOptions` configuration in `Program.cs`.
- Publication format marker updated to `1.22`; older publications remain loadable.
