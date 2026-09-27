# PublisherStudio 3.8.7 source validation

Source-only validation was performed. No dotnet/MSBuild/NuGet/publish/installer/GitHub operation was run.

Validated statically:
- all explicit `DxFormLayoutItem` templates have locally unique Context names;
- the maintained Razor architecture audit passes;
- DevExpress/DevExtreme remains pinned to 25.2.10;
- package/app/installer versions are aligned at 3.8.7;
- ZIP integrity is checked after packaging.
