# PublisherStudio v1.0.60 release

See `CHANGELOG-v1.0.60.md`, `CHANGELOG-v1.0.59.md`, and `VALIDATION.md`.

Gallery controls in the main publication frame now have deterministic pointer ownership. A selected Gallery's navigation buttons and indicators no longer compete with canvas object dragging, designer swipe recognition is disabled, navigation settles on exactly one item, and the current item survives harmless runtime rerenders. Exported HTML keeps normal Gallery swipe and animation behavior.

The **View > Zoom** ribbon now lets users choose between **Sharp CSS layout (default)** and the previous **Compact transform** renderer. The choice is stored with the publication/template. Ordinary document content uses CSS layout zoom in supported Chromium/Edge builds for sharp UHD rendering; DevExtreme Professional Components retain the transform-compatible path to protect widget pointer and layout calculations. Export and print rendering are unchanged.

Application and installer version `1.0.60`; publication format `1.47`; picture format `1.2`. There is no separate Media Host executable or release payload.
