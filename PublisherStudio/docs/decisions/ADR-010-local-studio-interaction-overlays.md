# ADR-010: Local Studio interaction overlays

**Status:** Accepted for PublisherStudio v1.0.69.

## Context

Native video controls consumed pointer input before Video Studio's frame-region workflow, while previous PublisherStudio regressions showed that solving such conflicts with broad Z-index changes can break Mainframe selection, connector and layer orchestration. Picture selections also needed an obvious selected area and cursor without becoming persisted layers. Recording finalization exposed a separate range error when a fixed minimum exceeded a very short clip duration.

## Decision

Spatial editing uses a transient overlay inside the owning Studio surface:

- Video Studio positions its overlay over the actual rendered source-frame rectangle after `object-fit`, darkens the remaining preview, blocks native player input only while frame-region mode is active, and keeps pointer-move feedback in JavaScript/CSS.
- Picture Studio renders its selection veil, boundary and nodes after document layers but does not add them to `PictureDocument.Layers`; its HTML guide is pointer-transparent.
- Audio Studio remains temporal only.
- Overlay Z-order is local to an isolated stacking context. No global Z-index or publication-layer mutation is allowed.
- All observer and DOM event bindings have deterministic cleanup.
- Media trim ranges are normalized before clamping, and UI minimum spans may never exceed the finite clip duration.

## Consequences

- Native controls and selection tools cannot own the same gesture.
- Selection feedback is visible without changing canonical document composition.
- Video selections map correctly across letterboxing and source aspect ratios.
- Very short recordings can finalize without `Math.Clamp` receiving an invalid minimum/maximum pair.
- Mainframe Z-order, element identity, connectors and animation orchestration remain unchanged.
