# PublisherStudio v1.0.38 release

See `CHANGELOG-v1.0.38.md`.

Source release notes:

- The publication/video-cut timeline no longer accumulates stale playback loops or delayed playhead callbacks after repeated Play, Pause, Stop, scrub, or timeline reopen operations.
- Every playback run has an identity shared by JavaScript and Blazor. Old callbacks cannot update or terminate a newer run.
- JS-to-.NET playhead reporting is coalesced to one in-flight callback, preventing render backlogs from making the playhead appear to accelerate.
- Clip dragging and trim handles are bounded to the visible timeline track and reject non-finite values without changing valid cut, move, playback-rate, fade, loop, or source-duration behavior.
- Run `npm run test:timeline` from `src/PublisherStudio.Web` to execute the timeline lifecycle regression test.
- Run `Prepare-DevExpressAssets.cmd` on the licensed build machine before publishing, as in v1.0.37.
- Publication document format remains `1.36`; existing projects remain compatible.
