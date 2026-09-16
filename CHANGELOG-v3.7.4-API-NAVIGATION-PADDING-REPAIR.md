# PublisherStudio 3.7.4 — API navigation padding repair

## Scope

This is a deliberately narrow PublisherStudio documentation-shell repair. It addresses the reported left-navigation spacing mismatch inside API reference pages and advances the release metadata without changing the editor/runtime architecture, application behavior, or documentation visual language.

## Fixed

- Managed-reference/API pages now give the left `.toc-offcanvas .offcanvas-body` the same `.8rem .65rem` padding used by the Overview and Guide navigation paths.
- The selector is scoped to `body[data-yaml-mime="ManagedReference"]`, so conceptual Overview/Guide layout and unrelated page geometry are not changed.

## Synchronization

- Advanced PublisherStudio to 3.7.4 across the web project, installer project, package metadata, application asset cache keys and maintained documentation metadata.
- Synchronized the maintained documentation CSS with both shipped help-doc copies.
- Updated generated help-page CSS cache keys to the current maintained stylesheet hash.
- Synchronized the tracked Pages documentation snapshot, including the 3.7.4 handbook reference.

## Regression protection

- Added `build/audit_release_3_7_4.py` to verify the ManagedReference navigation selector/padding, release versions, shipped CSS equality, generated help cache keys and tracked Pages snapshot.
- Existing component-resilience, cross-platform-boundary, prerender-interoperability and service-resilience static audits continue to pass.
- No .NET build, restore or publish was performed for this source-only handoff, per the supplied environment constraint.
