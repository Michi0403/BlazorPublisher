# ADR-009: Editor gesture and Z-order ownership

**Status:** Accepted for PublisherStudio v1.0.68.

## Context

Video Studio, Audio Studio and Picture Studio need timeline selections, cutlines, polygon regions, copy/paste and touch gestures. PublisherStudio has also previously suffered regressions when embedded controls, overlays and the Mainframe competed for pointer ownership or accidentally changed page composition.

## Decision

Studio gesture modes are explicit and mutually exclusive. Components own transient pointer/mode state; reusable Services own deterministic media timeline or document mutations; Domain/Models own the persisted contracts.

Temporal media sections remain a sequence inside one `PublicationMediaElement`. Frame and picture polygons remain properties of the owning element/layer. Playheads, cutlines, region outlines and nodes are editor-local overlays and never become publication elements or Z-order participants.

The Mainframe remains the only owner that inserts or updates publication elements. Applying an editor result preserves placement identity, Z-index, groups, connectors, animations and interactions. Every global listener and pointer capture has a deterministic cleanup path.

Audio supports one-dimensional temporal selection only. Video supports temporal selection plus normalized two-dimensional frame polygons. Picture Studio supports document-coordinate polygons so arbitrary angles and dimensions remain stable through layer transforms and exports.

## Consequences

- Gesture bugs can be isolated by active mouse mode and owning component.
- Internal clip edits cannot disturb page Z-order or connector orchestration.
- The same canonical data drives editor preview, Mainframe, print and export.
- Studio implementations require disposal and export-contract tests, not only visual manual checks.

## Additional invariants

- Audio editing is temporal only; spatial overlays are not generalized into audio.
- Video polygons are normalized to the source frame, while Picture Studio polygons remain in the picture document coordinate system.
- Mainframe update paths reuse the selected publication element rather than replacing it, and media asset-cache ownership follows canonical segment identifiers rather than transient preview identifiers.
