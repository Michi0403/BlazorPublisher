# PublisherStudio v1.0.56 release

See `CHANGELOG-v1.0.56.md`, `CHANGELOG-v1.0.55.md`, `docs/STREAMING.md`, and `VALIDATION.md`.

Animation Preview now clears its previous run and captures stable authored transforms before scheduling animations. Repeated preview or loop triggering restarts cleanly instead of applying transform effects on top of the prior preview.

Publication-owned editor settings now persist reliably: zoom, ruler unit, grid and guide visibility, all snapping options, grid spacing, and export DPI mark the document modified and travel with saved publications or templates. Streaming settings are intentionally removed from publication JSON and kept in an encrypted Local Application Data store keyed by publication ID; legacy embedded settings are migrated locally.

Professional Components `Map` objects now reserve designer gestures for the publication canvas. Their map provider cannot pan while the object frame is selected or moved, including at non-default canvas zoom and in connector-heavy layouts. Presentation and exported maps remain interactive.

Application and installer version `1.0.56`; publication format `1.45`; picture format `1.2`. There is no separate Media Host executable or release payload.
