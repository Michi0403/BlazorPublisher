# PublisherStudio v1.0.20 object workflow and recovery stabilization

- Added desktop file drag and drop for pictures, videos, plain text, and Markdown. The publication shows a live object preview at the pointer position while dragging, uploads the original file without an application size cap, and leaves the converted publication object where it was dropped.
- Normal insert commands now remember the last clicked publication position. Text, pictures, video, audio, shapes, WordArt, barcodes, and data visuals use that point when no explicit drag position is supplied.
- Separated **Insert picture** from **Replace picture** so insertion always creates a new frame while replacement remains an explicit command.
- Made alignment and layer ordering group-aware. A persistent group moves to page alignment positions and changes z-order as one block while preserving the order of its members.
- Made canvas previews, timeline playback, HTML presentations, and video export animate grouped objects together. Scale and rotation effects share the visual center of the group.
- Stabilized DevExpress chart layering by retaining a fixed component/DOM order and changing only CSS z-index values. Bringing text or another object forward no longer recreates and empties the chart component.
- Increased default chart/data-visual dimensions, introduced useful type-specific minimum sizes, and normalizes undersized visuals when older publications are opened.
- Replaced the WordArt free-text font field with the same practical font choices used by the other editors.
- Fixed timeline range/clip dragging so pointer release, lost pointer capture, window release, cancellation, and focus loss all end the operation. The visible range now commits on handle release rather than continuously rebuilding the timeline while dragged.
- Added local atomic recovery snapshots under the user's application data. Unsaved work is restored through a startup banner, the browser warns before leaving dirty work, and HTML/video exports create a recovery point before taking over the presentation surface.
- Added a visible **Cancel export and return** command to presentation video export. Escape, ending tab capture, or pressing Cancel returns focus to the publisher workspace and tears down capture/preview resources.
- Kept the v1.0.19 maximum-page frame calculation, portrait/landscape behavior, proportional page fitting, transition playback, and video resolution unchanged.
