# PublisherStudio 4.0.4 source validation

## Scope

DevExpress-first editor repair plus restoration of direct DOM ownership for Panel Studio and other geometry-sensitive structural renderers.

## Confirmed source checks

- PublisherStudio Web, InstallerConsole, npm package/lock and browser cache-buster identities are aligned at **4.0.4**.
- `SystemFontPicker` contains no native input/select/datalist and uses a searchable `DxComboBox` with custom user input and virtual list rendering.
- Maintained PublisherStudio Razor source contains **zero `<datalist>` controls** and **zero native `input type="color">` controls**.
- Page Appearance & Effects uses DevExpress editor/action controls for its visible interactive UI.
- `PanelView` is an explicit `razor-structural-renderer` with a direct `.publication-panel` root and direct `.publication-panel-view` authored views; the geometry-sensitive renderer set no longer owns maintenance FormLayout wrappers.
- Panel Studio browser-native drag-source and hit-test buttons remain narrowly documented interaction primitives rather than ordinary application editor widgets.
- The DevExpress retention manifest covers every maintained Razor file and prevents increases in native interactive/disclosure debt while preventing DevExpress component-count regression.
- The Panel Studio authoring geometry and Razor maintenance guards encode the structural-renderer repair so wrapper insertion cannot silently regress authored geometry again.

## Source audits executed

The maintained Python audits for Razor maintenance architecture, async-only architecture, async continuation ownership, component resilience, service resilience, Panel Studio persistence, prerender interop safety, iterator exception policy, cross-platform boundaries and transient UI-state ownership passed on this source tree. JavaScript/JSON/XML syntax checks and the release-specific audit are rerun before packaging.

## Environment limitation

The .NET SDK/MSBuild and PowerShell runtime are not used in this preparation environment. No restore/build/publish, Windows/macOS/Linux native package build, notarization, or runtime UI execution is claimed. The maintainer's .NET 10 + DevExpress build remains authoritative for compiler/runtime verification.
