# PublisherStudio 3.7.0 — Documentation starfield and Mermaid recovery

PublisherStudio 3.7.0 completes the documentation visual recovery without changing editor/runtime architecture or the established Blazor render-mode boundaries.

- Restores a visible non-tiled deep-space background even when the JavaScript sky is delayed. A non-repeating CSS fallback provides uniquely positioned stars while the dynamic layer adds 112 randomized desktop stars (48 compact), independent brightness/twinkle/drift timing, white/lavender/pink/blue/warm points and cross-stars, drifting nebula haze, small satellites, and an occasional ringed planet.
- Keeps navigation rails, article surfaces, cards, API panels, details and tabs visibly translucent with backdrop blur instead of flattening them into opaque purple blocks.
- Retains the symmetric desktop viewport gutter and explicit right-rail padding so the article navigation does not sit against the browser edge.
- Reworks Mermaid recovery around direct `mermaid.render(...)` calls through DocFX's bundled module and removes the hidden-element `offsetParent` dependency. Architecture diagrams can therefore render while the in-app documentation viewer is initially hidden instead of exposing raw `flowchart` source.
- Explicitly fences the maintained architecture flowcharts as Mermaid and gives rendered SVG diagrams full-width responsive glass styling.
- Preserves `prefers-reduced-motion` and print/PDF behavior: decorative motion stops for reduced-motion users and the website-only sky is hidden for print.
- Keeps the versioned documentation PDF required by default, and synchronizes authored CSS/JavaScript with both in-app help asset locations and the tracked Pages snapshot without relabeling historical generated PDF/API payloads.
- Advances the release identity from 3.6.9 to 3.7.0 in accordance with the one-digit minor/patch version rule.
- Retains the existing `InteractiveServer` page/diagnostics boundaries and intentionally static error page.

Visual frontend checklist: expected appearance is the varied moving deep-space field behind readable glass panels, full Mermaid SVG diagrams rather than raw flowchart text, and comfortable symmetric desktop edge spacing; reduced-motion retains the look without animation; print removes decorative sky layers.

No .NET build, restore, publish, signing/notarization, GitHub access, or GitHub API operation was performed for this source-only release.
