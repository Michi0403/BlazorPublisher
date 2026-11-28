# PublisherStudio 3.0.6 source validation

This handoff was validated without invoking `dotnet` and without GitHub access.

Static checks cover:

- release and npm identity at `3.0.6` and the single-digit minor/patch version rule;
- generated DocFX HTML language normalization occurring in the existing theme pass before the strict pre-PDF accessibility validator;
- preservation of native discovery resolution hardening and platform guards from 3.0.5;
- existing cross-platform and InteractiveServer source audits;
- ZIP integrity and repository-root layout.

A real .NET release build still has to be run on the target build machine.
