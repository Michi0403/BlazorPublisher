# PublisherStudio 4.5.6 offline validation

- Node test suite `tests/js/*.test.cjs` and `tests/js/*.test.mjs`: 17 tests passed.
- `node --check docs/templates/publisherstudio/public/main.js`: passed.
- Headless Chromium fixture: paw renders at ~54.39px on fine pointer; distinct theme-resolved dark rim/light shadow; cursor remains positioned by existing pointer events; existing dragged claw masking remains operational. No page JavaScript exceptions in the isolated fixture.
- No .NET build, DevExpress runtime, or full installed DocFX regeneration run.
- No maintenance baselines or build guard files changed.
