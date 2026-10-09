# PublisherStudio 4.1.7 — PanelDocumentService XML documentation repair

PublisherStudio 4.1.7 is a focused build-validation follow-up to 4.1.6.

- Added the missing XML documentation for the `PublicationDocument document` parameter on `PanelDocumentService.CreateBlank(PublicationDocument document, string name = "Panel")`.
- No Panel Studio runtime behavior from 4.1.6 was removed or changed; blank-panel prerequisite seeding remains intact.
- Source XML documentation coverage validation now passes for the supplied tree.

No .NET build, restore, publish, or GitHub access was used while preparing this source release.
