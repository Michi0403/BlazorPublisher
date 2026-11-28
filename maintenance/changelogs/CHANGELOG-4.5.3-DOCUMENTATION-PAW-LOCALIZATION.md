# PublisherStudio 4.5.3 — localized GIF action and documentation claw tears

## Fix: Visual Studio L10N0003

- Add the exact canonical `Phrase.Apply␠animated␠GIF` entry corresponding to `LT("Apply animated GIF")` in `PictureEditor.razor` to **all six shipped cultures**: en-US, de-DE, es-ES, fr-FR, ja-JP and uk-UA.
- German: **Animiertes GIF anwenden**. Other languages also contain meaningful translations. The guarded Razor localization call and `Assert-LocalizationIntegrity.ps1` remain unchanged; no exemptions or maintenance baseline updates.

## Documentation Easter egg

- Use the chosen glossy multicolor paw (Option 2) at about 2.5× the old cursor size, with dark/light-adaptive glow.
- Implement Option 3-inspired accumulating torn-paper claw cuts through the rendered DocFX page content along held-primary-button drags, plus existing short click claw prints, jagged paper edges, lifted fibers and theme-sensitive front-glow/underlayer.
- Keep the actual document and its navigation untouched. Separate ephemeral SVG alpha mask, decorative overlay and user-triggered varied Web Audio scratching; reset on refresh/navigation. Avoid overriding controls; preserve Shift+drag text selection and existing scratch burst/paw trail.
- No PublisherStudio editor canvas logic, renderer, documentation builder, DevExpress components, generated HTML, localization validator, or release guard is modified.

## Verification and limits

- 13/13 PublisherStudio JavaScript tests pass; all six JSON catalogs parse with the new exact key.
- Browser-driven Chromium tests exercise both themes, real masking of text in an isolated simulated DocFX page, drag behavior, text selection prevention and reset on navigation/reload. They do **not** prove that all production-generated DocFX layouts behave identically.
- No .NET build or PowerShell maintenance guard run. Rebuild generated documentation in the existing pipeline to deploy the new effect; pre-generated snapshots were intentionally left untouched.
