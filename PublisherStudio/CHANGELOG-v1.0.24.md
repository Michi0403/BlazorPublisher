# PublisherStudio v1.0.24

- Restored mouse marquee selection on blank page areas. The rectangle selects every intersecting visible object across all z-levels and expands any hit object to its complete persistent group. Ctrl, Shift, or Command keeps the existing selection while drawing the rectangle.
- Added one exact server reconciliation call for marquee selection, preventing the browser selection preview and Blazor selection state from drifting apart after the pointer is released or cancelled.
- Reworked Story Editor preview transfer for image-containing stories. RichEdit HTML is streamed to JavaScript, normalized there, and returned to Blazor in small ordered chunks, so embedded pictures no longer exceed the interactive-server message limit when **Apply to frame** is used.
- Waits for embedded story images before producing the canvas/print representation, preserving their final layout.
- Added print-safe color fills for story page backgrounds, paragraph shading, text highlighting, table-cell fills, text-frame backgrounds, and publication page backgrounds. The fallback uses printable inset fills in addition to normal CSS backgrounds, so Edge/Chromium print preview keeps the colors even when its background-graphics option is disabled.
- Retained foreground text colors, DOCX source fidelity, external/internal clipboard handling, drag-and-drop, keyboard commands, grouping, recovery, animations, chart stability, and the existing HTML/video export sizing.
- Publication format marker updated to `1.24`; older publications remain loadable.
