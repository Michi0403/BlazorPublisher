# PublisherStudio 4.4.4 — Media AI workflow and Panel Library UX

## Baseline

Complete PublisherStudio **4.4.3** source, including the earlier macOS documentation cache/Debug-gate fix and 1-Wire Panel/Div organic module bridge. No architecture assertion or approval rule was removed or weakened.

## Changes

- Reorganized Insert ribbon: **Media** now contains Picture, Create picture, video/audio insertion and creation, plus linked-only **Create picture/video/audio with AI** actions. Removed picture creation from Objects.
- Replaced the separate Panel Library/blank panel items with a single **Panels** dropdown containing Panel Library, Insert blank panel, Interactive 3D blob, and connected-only panel-template and 3D AI actions. Existing native non-AI Studios remain callable independently.
- Enlarged Panel Library to an approximately **1080 px** responsive workspace dialog/dock with larger preset cards. Added **Create templates by prompt**, visible only while the linked `council.run` capability is available.
- Added a native **DevExpress DxPopup** media/panel AI surface, with a typed prompt, one user-initiated Council request, reviewable response, verified-model discovery request, and an explicit Open native Studio action.
- Integrated the existing protected `IPublisherAiBridgeService` Council entrypoint. Media workflows negotiate the new image/video/audio/panel Council seeds when advertised by LocalGPT and otherwise fall back to `general` for older connected peers.
- The **Find compatible models** action asks LocalGPT to inspect its installed model inventory before initiating *a separate human-approved* CanIRun.ai hardware-fit lookup. No hardware data is sent automatically. The UI does not display hard-coded or fictional scores.
- Extended all six localization catalogs equally; existing JS manifests and picture editing runtime are unchanged.

## Security / completion boundaries

- Council text is **not** treated as a generated picture/video/audio or automatically inserted into a publication. Media generation remains gated by installed models and LocalGPT DX-function approvals; the existing bridge returns text/structured Council summaries, not a verified PublisherStudio-importable binary asset stream.
- The prompt-based Panel Library action currently creates a **reviewable native authoring request** and can open Panel Studio. Automatically validating/persisting arbitrary AI-generated template JSON into the local template library still requires a dedicated reviewable canonical import transaction.
- True media artifact import into PublisherStudio, automatic creation of Studio timelines/pictures from a finished model artifact, true streaming tokens into media authoring, and actual end-to-end Council/DevExpress testing remain outstanding.
- PublisherStudio does not claim any user-supplied CanIRun.ai model is installed or that sample scores are measured locally.

## Tests

See `VALIDATION-v4.4.4-source.md` and `tests/js/ai-media-panel-ribbon.test.cjs`.
