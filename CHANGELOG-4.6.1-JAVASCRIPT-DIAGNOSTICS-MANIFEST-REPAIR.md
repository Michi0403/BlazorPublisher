# PublisherStudio 4.6.1 — JavaScript diagnostics hash-manifest release repair

The user's Windows build failed `Assert-JavaScriptDiagnostics.ps1` because the maintained `automationInterop.js` had changed without an updated reviewed manifest. Recomputed the normalized SHA-256 manifests for `automationInterop.js` and the already-divergent `javascript-diagnostics.js`; verified all maintained JavaScript files against the unchanged PowerShell guard. No browser automation implementation or network/security rule was changed in this release. Preserved 4.6.0 live canvas dragging and notification wiring.

Validation: 21/21 PublisherStudio Node.js source/browser fixture test files pass; all 17 reviewed maintained JavaScript SHA-256 checks match. The user-observed NuGet restore failure could not be reproduced or resolved without an actual Windows/Visual Studio environment; no .NET or PowerShell build was performed.
