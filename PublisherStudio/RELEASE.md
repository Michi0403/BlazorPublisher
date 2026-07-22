# PublisherStudio v1.0.58 release

See `CHANGELOG-v1.0.58.md`, `CHANGELOG-v1.0.57.md`, and `VALIDATION.md`.

Map and Vector Map editing now uses two explicit, mutually exclusive mouse modes. **Move map object** gives the publication canvas complete drag ownership and blocks native map gestures. **Pan / zoom map content** gives gestures only to the selected map and prevents the publication frame from moving.

The current mode is visible in the canvas mouse indicator, Component Tools ribbon, and context menu. Selection changes leave content mode automatically, unselected maps cannot retain pointer ownership, and manually changed map center/zoom values are saved with the publication.

Application and installer version `1.0.58`; publication format `1.45`; picture format `1.2`. There is no separate Media Host executable or release payload.
