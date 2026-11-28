# PublisherStudio 2.6.3 source validation

This package was prepared without GitHub access and without invoking dotnet/MSBuild. The owner-side Windows build remains the compiler/runtime authority.

Source/static checks performed:

- Dedicated 2.6.3 preview/AI/export UX regression audit: passed for custom preview presets, Panel Studio inspector/footer UX, nested-scroll tooltip handling, embedded culture selector, single-file media optimization, StoryEditor selection AI, LocalGPT-backed DevExtreme Chat, publication format, and release versions.
- PublisherStudio 2.6.2 picture/page-effect regression audit: passed unchanged.
- PublisherStudio 2.6.0 data/panel/media regression audit: passed unchanged.
- Panel Studio persistence/geometry lifecycle audit: passed unchanged.
- Application architecture audit: passed.
- Service resilience audit: **1,274** covered service methods passed; four iterator methods and four direct Program/Startup methods remain intentionally excluded.
- PublisherStudio documentation/1-Wire contract audit: passed.
- XML documentation validation: passed for **5,489** direct C# declarations across **183** maintained source files.
- XML documentation enrichment: repeated pass made **0** changes.
- Localization integrity emulation: **3,119** unique English keys and **3,119** unique German keys, exact key parity and no case-insensitive duplicates.
- JavaScript diagnostics guard emulation: passed for all **16** maintained top-level PublisherStudio browser JavaScript files with matching normalized SHA-256 inventory.
- JavaScript syntax: `node --check` passed for all **16** maintained top-level browser JavaScript files.
- Project/build XML and application JSON parsing: passed.
- Reviewed InteractiveServer page boundaries remain unchanged.
- Publication format is **1.58**; PublisherStudio.Web and PublisherStudio.InstallerConsole are both **2.6.3**.

Additional policy checks were reviewed during the implementation pass for component diagnostics, async continuations, system-variable initialization, and text-service ownership; the new code was adjusted to preserve those maintained boundaries.

Not executed:

- dotnet build / MSBuild
- DocFX
- runtime browser automation
- installer execution

Those remain for the owner's Windows build/test environment.
