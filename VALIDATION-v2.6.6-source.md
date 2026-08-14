# PublisherStudio 2.6.6 source validation

Source-only validation was performed without invoking dotnet, MSBuild, Visual Studio builds, or GitHub. The user's Windows build remains authoritative.

## Async continuation invariant

- 75 maintained C#/Razor source files contain async constructs.
- 1,039 `await` tokens were reviewed by the syntax-aware audit.
- 423 continuations use `ConfigureAwait(false)`.
- 562 ordinary renderer-affine continuations use `ConfigureAwait(true)`.
- 49 `await using` disposals are explicitly configured: 26 false and 23 true.
- 5 async streams are explicitly configured.
- Raw/unconfigured awaits: **0**.
- Active reviewed-count/baseline file: **none**.

## Architecture and regression checks

- Application architecture audit: passed.
- Service resilience: 1,278 covered service methods passed; 4 yield methods and 4 direct Program/Startup methods remain policy exclusions.
- Text-service ownership: no new direct component/controller string/regex operations.
- XML documentation: 5,498 direct declarations across 183 maintained C# files.
- Panel Studio persistence: passed.
- Data/panel/media regression suite: passed.
- Picture Studio/page-effects regression suite: passed.
- AI/preview/export regression suite: passed.
- Media Studio/drag/effect/localization regression suite: passed.
- 2.6.6 strict async/build-policy regression suite: passed.
- Documentation/1-Wire audit: passed.
- Localization: 3,251 unique keys in each maintained culture catalog; case-insensitive duplicates absent and EN/DE key parity preserved.
- Browser JavaScript: all 19 checked files pass Node syntax validation.
- Project/build XML, JSON configuration and ZIP-source structure checks: passed.
- Modified Razor render-mode directives are unchanged from 2.6.5.

## Versioning

- PublisherStudio.Web: 2.6.6.
- PublisherStudio.InstallerConsole: 2.6.6.
- Publication format: 1.58 (unchanged).
- Picture Studio format: 1.5 (unchanged).
