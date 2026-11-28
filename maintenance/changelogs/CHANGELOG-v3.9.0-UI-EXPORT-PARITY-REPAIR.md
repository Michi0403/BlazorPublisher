# PublisherStudio 3.9.0 — UI/export parity repair

- Preserved the recovered publication Mainframe, rulers, page surface, pane splitters, and box-neutral DevExpress maintenance wrappers from 3.8.9.
- Repaired Data Visual chooser contrast: DevExpress primary-button semantics remain, while local light-surface captions/descriptions receive explicit Bootstrap/Office-compatible colors and selected state styling.
- Repaired Data Visual checkbox alignment so boolean controls remain horizontally paired with their labels/values.
- Restored rectangle `CornerRadiusMm` behavior in PageSurface, PanelView, and PrintPublication/export rendering.
- Re-sized picture/website export popups to bounded responsive footprints and made website-export content fill/scroll inside the popup body.
- Restored presentation control-bar resilience: controls default on unless explicitly disabled and render above publication content; print still hides them.
- Added `UI-STYLE-GUIDE.md` documenting control contrast, checkbox alignment, modal geometry, geometry/export parity, box-neutral DevExpress wrappers, and render-mode preservation.
- Advanced PublisherStudio Web, installer, npm metadata, and browser cache keys from 3.8.9 to 3.9.0.
