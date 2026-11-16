# PublisherStudio 4.8.9 — release fixes from the MacBook Neo run

- Installer console version aligned with the application (was 4.8.7 vs 4.8.8 and blocked the release).
- RID release publish no longer fails in MSBuild because wwwroot/help-docs is missing: Build-Release.ps1 publishes without docs, copies the verified shared docs and validates them itself (Assert-PublisherStudioDocumentationPayload), and now tells MSBuild so with PublisherStudioDocumentationValidatedByReleaseScript=true. Normal publishes keep the MSBuild check.
