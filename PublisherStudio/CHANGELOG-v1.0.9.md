# BlazorPublisher v1.0.9 — unified interaction and ribbon stabilization

## Fixed

- Restored reliable double-click activation with an element-ID based click detector that survives Blazor rerenders between the first and second click.
- Restored connector drawing and reconnection: connector mode is now explicit, ports stay above object handles, handles are hidden while connecting, and the destination is resolved again on pointer release.
- Connector mode no longer disappears as an accidental side effect of selecting an object; use Done, Escape, or the ribbon command to leave it.
- Direct video insertion now inspects the exact uploaded bytes through the local ranged media endpoint before adding the object.
- Newly imported or recorded Video Studio clips remain open until the media asset has been copied into the publication, fixing the new-clip insertion regression.
- Barcode and QR format, error-correction, and module-style values now handle numeric .NET enum serialization correctly instead of silently falling back to Code 128.
- Barcode generation ignores stale asynchronous preview results when settings are changed quickly.
- Opening file pickers and modal editors cancels any unfinished publisher pointer operation first.

## Changed

- Media Studio, Story Editor, Barcode Studio, Publication Data, Data Visual Editor, and the publication timeline now use DevExpress ribbon command surfaces.
- Picture Studio output commands are available from an Output ribbon tab; dialog footer actions remain available as standard confirmation controls.
- Command-surface styling is shared across the main publisher and editor studios for a consistent desktop workflow.
