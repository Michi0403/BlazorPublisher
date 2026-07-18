# PublisherStudio v1.0.21 interaction, story and canvas-state stabilization

- Added complete canvas keyboard commands after the canvas has been activated: copy, cut, paste, duplicate, delete, select all, group, ungroup, undo, redo, and precision arrow-key movement. Editor inputs and modal studios keep their normal keyboard behavior.
- Reworked the publication clipboard to preserve multi-object selections, relative positions, group membership, connectors between copied objects, interactions, animations, and browser-backed media assets. Mouse and keyboard paste now select the complete pasted set.
- Hardened pointer-state recovery after insert, paste, model revisions, lost capture, cancellation, and animation preview. Transient DOM movement, snap overlays, file-drop previews, insert ghosts, connector state, and animation interception are cleared together before normal editing resumes.
- Animation previews no longer leave the canvas in a click-capturing state. Clicking the canvas cancels preview playback and restores normal selection; completed non-interactive previews clean themselves up.
- Added a live object-shaped drag preview for the Text, Picture, and Video quick-insert controls. The preview follows the pointer over the publication page and the inserted object or selected media workflow uses the final drop position.
- Improved desktop file drag detection for browsers that expose file metadata during dragover but provide the full File object only on drop. Existing unrestricted picture, video, text, and Markdown import remains in place.
- Story HTML preview generation now resolves DevExpress RichEdit CSS into inline computed styles before saving the frame preview. Text colors, highlighting, fonts, paragraph formatting, lists, tables, borders, spacing, and page/background colors therefore survive canvas display, publication printing, HTML export, and video export. A style-preserving server fallback remains available when JavaScript is disconnected.
- Removed layout containment from DevExpress data-visual hosts because it could blank chart SVG/canvas content after z-order changes. Stable component keys, parent-only z-indexing, minimum sizes, and layout refresh behavior remain unchanged.
- Extended exact print-color handling to all publication and story descendants.
- Kept the v1.0.20 atomic recovery system and the v1.0.19 shared HTML/video export frame calculation unchanged.
- Publication format marker updated to `1.21`; older publications remain loadable.
