# PublisherStudio 3.0.5 source validation

This handoff was validated without invoking `dotnet` and without GitHub access.

Static checks cover:

- release, npm, documentation, and browser-cache identity at `3.0.5`;
- the single-digit minor/patch version rule;
- fully qualified native discovery runtime-pattern references;
- nullable documentation-cache path guarding;
- explicit Windows rejection before `File.SetUnixFileMode`;
- existing cross-platform boundary and InteractiveServer source audits;
- ZIP integrity and repository-root layout.

A real .NET release build still has to be run on the target build machine.
