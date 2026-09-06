# PublisherStudio 3.3.9 source validation

Validation is source/static only in this environment. No .NET build, macOS signing, Apple upload/notarization, package publication, or GitHub access was performed.

Checked invariants:
- version references advance to 3.3.9 with no `x.y.10` version;
- all normal routed PublisherStudio pages retain their reviewed `@rendermode InteractiveServer` boundaries and the error fallback remains intentionally static;
- `Build-Release.ps1` re-runs `Initialize-MacReleaseTrust.ps1` immediately before each `osx-*` release lane;
- fresh native notarization validates the keychain immediately before `notarytool submit ... --wait --timeout ...` and retries only Apple's explicit missing-keychain-item failure once;
- prior `.notary-state.json` resume remains available through status polling;
- the equivalent `html-browser-chunked` PDF validation/completeness fix from 3.3.8 remains intact;
- no repository-local build output was intentionally introduced.
