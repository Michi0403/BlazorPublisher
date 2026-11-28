# PublisherStudio 4.1.6 — blank Panel Studio prerequisite seeding

## Scope

This release repairs the difference between a newly blank Panel Studio surface and the pre-seeded presets. The blank surface remains visually empty, but its owning publication document is now prepared with the same publication/data prerequisites that dynamically added Panel Studio components depend on.

## Repair

- The Panel Studio initial blank path now calls the document-aware `CreateBlank(PublicationDocument, ...)` overload.
- Blank creation seeds built-in publication objects through the existing publication data service and ensures a usable non-built-in data object exists for later data-bound components.
- `EnsureData` now first guarantees built-in publication objects and deliberately selects a non-built-in data source; if none exists it creates the same sample-data prerequisite used by the working preset path.
- Dynamically added nested panels use the document-aware blank creation path as well.
- Dynamically created palette elements are explicitly visible on creation, preserving the existing add-on-the-fly behavior.
- Existing presets continue through the same normalization/persistence path; no parallel panel model or renderer was introduced.

The canvas is not populated with arbitrary visible widgets merely to make it appear non-empty. The fix prepares the shared data/object infrastructure so the user's subsequent palette additions work the same way they do in pre-seeded panels.

DevExpress/DevExtreme remains **25.2.10** and .NET remains **10.0.12**.
