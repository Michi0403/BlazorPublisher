# PublisherStudio 3.6.9 source validation

This is a source-only validation record. No .NET build, restore, publish, signing/notarization, GitHub access, or GitHub API operation was performed.

Validated statically:

- PublisherStudio Web, installer-console, and package metadata identities are `3.6.9`; the one-digit minor/patch version rule is satisfied.
- Current DocFX metadata, PDF filename metadata, and application JavaScript cache-busting identity use `3.6.9`.
- `Directory.Build.targets` defaults `RequirePublisherStudioDocumentationPdf=true`; HTML-only documentation now requires an explicit `false` override.
- `Build-Documentation.ps1` retains the existing required-PDF validation/runtime-copy path and now describes disabled PDF only as an explicit HTML-only diagnostic build.
- `docs/templates/publisherstudio/public/main.js` passes `node --check` and contains the bounded hidden-viewer Mermaid recovery using the bundled DocFX Mermaid module.
- The generated sky contract contains 112 desktop / 48 compact randomized stars, non-tiled nebula layers, bounded satellites, an occasional planet, and reduced-motion support.
- The final theme disables the old tiled pseudo-element star textures, uses materially translucent glass variables/backdrop filtering, and applies the symmetric desktop viewport gutter/right-rail padding.
- Authored Kawaii CSS/JavaScript are byte-identical to both checked-in runtime asset locations and to the corresponding assets inside `.github/pages/publisherstudio-kawaii-docs.zip`; injected HTML cache keys were refreshed.
- The tracked Pages ZIP's older generated API/PDF payload was not renamed or represented as 3.6.9. A normal owner-side documentation build is now required to regenerate that payload and will fail if the current versioned PDF is absent.
- The established render-mode ownership set is unchanged: the four routed interactive pages and diagnostics bridge keep their existing boundaries; `Error.razor` remains static and editor children continue to inherit the owning circuit.
- XML project/targets files plus current DocFX/package JSON parse successfully; Kawaii CSS braces are balanced.
- The maintained application-architecture, cross-platform-boundary, PowerShell interpolation, service-resilience, prerender-JavaScript-safety, and component-resilience audits pass.

No claim is made that C# compilation or PDF rendering itself was runtime-tested in this environment.
