# PublisherStudio 2.6.8 changelog

## Fixed

- Fixed blank/custom Panel Studio authoring where the preview-preset editor created an implicit grid row and pushed the real design canvas below the usable workspace. The authoring shell now explicitly allocates banner, preset editor, and fitted canvas rows.
- Non-persistent notification boxes now own a ten-second expiration lifecycle instead of remaining until manually dismissed. Notification collection access is synchronized for timer-driven dismissal.
- Added visible per-layer backward/forward controls directly to Picture Studio's Layers panel while retaining the existing drag/drop and footer ordering actions.
- Audio Studio now passes the shared sequence-timeline surface to the same media interop used by Video Studio, enabling the same timeline drag/drop path for audio projects.
- Export-dialog footer captions receive explicit normal, disabled, and primary-button text contrast so button text remains readable without hover.
- Presentation pages now treat embedded panels as pointer owners. Panel runtime binding also recognizes native interactive DataVisual/DevExtreme/live-source descendants, and presentation page-click advance no longer consumes clicks originating inside those owners.
- Panel DataVisual/media/image descendants explicitly regain pointer events in website/presentation output so hover and native component reactions are available inside panel containers.
- Browser WebM conversion now uses a compression-oriented resolution/quality bitrate curve and explicit audio bitrate. Structured export no longer embeds both a successful compressed video and its original source. If “keep source video if conversion fails or is not smaller” is disabled, a requested conversion may be retained even when it is not smaller.

## Localization

- Completed built-in key parity for German and English.
- Added/expanded Spanish (`es-ES`) and Japanese (`ja-JP`).
- Added French (`fr-FR`) and Ukrainian (`uk-UA`).
- All six catalogs contain the full current localization key set; technical names and literal protocol/tooling strings remain canonical where appropriate.

## Release policy

- PublisherStudio.Web: 2.6.8.
- PublisherStudio.InstallerConsole: 2.6.8.
- Publication format: 1.58 (unchanged).
- Picture Studio format: 1.5 (unchanged).
- 1-Wire protocol: 2.1.1 (unchanged).
- Existing InteractiveServer boundaries are unchanged.
