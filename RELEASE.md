# PublisherStudio 2.9.1

PublisherStudio 2.9.1 is a documentation-completeness release built forward from the 2.9.0 recovery-cancellation/WebM insertion source. It does not remove or replace the 2.9.0 runtime repairs.

## Toolchain state retained

- Target framework: `net10.0`
- DevExpress: `25.2.9`
- dotnet-ef tool: `10.0.11`
- Installer `Microsoft.Extensions.Logging`: `10.0.11`
- SDK policy: `10.0.301` minimum with `latestFeature` roll-forward
- LocalGPT 1-Wire protocol: `2.1.1`

## Documentation completeness

PublisherStudio now enforces contextual XML documentation for maintained C# and Razor source, including private implementation members and individual enum values. Razor component partial classes and declarations inside `@code` blocks are part of the same source-quality gate. Empty contract tags are rejected.

The release audit records 6,064 direct C# declarations and 3,311 explicit Razor `@code` declarations under the documentation gate.

See `CHANGELOG-v2.9.1-XML-DOCUMENTATION-COMPLETENESS.md` and `VALIDATION-v2.9.1-source.md`.
