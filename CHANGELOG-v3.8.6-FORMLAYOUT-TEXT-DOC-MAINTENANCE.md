# PublisherStudio 3.8.6 — FormLayout, text ownership and documentation maintenance

- Replaced mechanically generated component-level/former-section `DxStackLayout` wrappers with `DxFormLayout` while preserving existing content, controls and behavior.
- Added one containment-only `.razor-component-boundary` div around ordinary component roots so native markup may isolate CSS/size bleed without becoming the semantic layout owner.
- Strengthened the Razor maintenance guard to prefer `DxFormLayout` for editors, scanners, configuration/settings and field-oriented UI.
- The guard rejects generic StackLayout ownership, requires FormLayout ownership for form-like DevExpress editors, requires a documented reason for a non-Form primary layout, and rejects unexplained Grid-to-Stack nesting.
- Hardened `Assert-DevExpressComponentRetention.ps1` so accidental removal of the FormLayout-first semantic guard and its anti-shortcut checks is itself build-breaking.
- Preserved the bans on `<section>`, `<dialog>`, native/manual modal ownership and DevExpress-control removal.
- Moved Media Converter metadata formatting from the Razor component into `PublicationEditorTextService`.
- Moved Media Studio SVG point-list and dim-path construction into `PublicationEditorTextService`, preserving invariant numeric formatting and existing visual semantics.
- Refreshed XML documentation for generated render-helper methods; the maintained Razor XML documentation audit now passes instead of reporting hundreds of missing `<returns>` tags.
- Preserved component method diagnostics, InteractiveServer/prerender safety boundaries and DevExpress/DevExtreme 25.2.10 asset preparation.
