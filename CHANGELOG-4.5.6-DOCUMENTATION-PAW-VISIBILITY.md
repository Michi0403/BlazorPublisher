# PublisherStudio 4.5.6 — Documentation Paw Visibility

## Changed

- Refined the documentation paw cursor with a tiny dark-fur silhouette behind its iridescent toe/palm pads, fine fur-tip details, and a more legible light outline.
- Increased only the cursor artwork to 3.4rem, retaining the existing pointer position and noninteractive overlay.
- Dark theme: cool cyan/blue-violet silhouette glow. Light theme: softened plum/charcoal cast shadow with a faint blue accent rather than an overpowering white neon halo.
- Scoped entirely to the documentation theme SVG/CSS; existing scratches, animated effects, damage masking, refresh/navigation restoration, DocFX generation, main application popups, and JS diagnostics remain unchanged.
- Incremented the Web and InstallerConsole versions from 4.5.5 to 4.5.6.

## Offline verification

- 17/17 JavaScript regression tests passed, including the new dark/light paw styling contract.
- Chromium synthetic fixture tested cursor position and light/dark computed styles, and successful drag-cut masking.
- JavaScript syntax passed `node --check`.
- Windows/.NET build and actual installed DocFX output were not verified in this environment.
