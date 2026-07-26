# PublisherStudio v1.0.87

## Browser runtime template compiler compatibility

- Fixed `CS9007` in `BrowserRuntimeTemplateService.CreateBlobRuntime`.
- Replaced the C# interpolated raw string with a normal raw string plus one explicit, ordinal placeholder substitution for the serialized configuration payload.
- JavaScript braces, adjacent closing braces, and JavaScript template-literal expressions such as `${...}` are now treated only as JavaScript content and can no longer collide with C# raw-string interpolation delimiters.
- Kept `IBrowserRuntimeTemplateService`, its DI lifetime, its public method, and the generated browser runtime behavior unchanged.
- Added a regression contract that rejects interpolated raw strings in this template, verifies the payload insertion boundary, preserves the JavaScript template expression, and runs `node --check` against a generated runtime sample.

## Compatibility and task status

- **Closed:** the reported `CS9007` compiler blocker in `BrowserRuntimeTemplateService.cs`.
- **Closed and retained:** the v1.0.86 SDK default-content fix and all v1.0.85 interface/API, OpenSCAD, automation, localization, code-editor and render-export work.
- **Partial:** native .NET/Razor/DevExpress compilation can now continue beyond the reported raw-string failure; any next compiler or package finding still requires the licensed developer-machine build.
- **Deferred:** previously tracked work remains open until a later tested release closes it, including the visual OpenSCAD builder, native OpenSCAD process execution, operating-system-global input, complete localization migration, full IDE/LSP editing and remaining static-to-service evolution.

The maintained task-status source remains `docs/architecture/task-ledger.md`.
