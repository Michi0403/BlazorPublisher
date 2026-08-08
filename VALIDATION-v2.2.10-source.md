# PublisherStudio 2.2.10 source validation

This package is source-only and was not compiled in the packaging environment.

Static validation performed before packaging:

- application architecture audit passed;
- service resilience audit passed: 1243 guarded service methods, with the maintained iterator/boot exclusions;
- documentation/1-Wire contract audit passed;
- JavaScript syntax validation passed for the maintained browser scripts;
- the user-confirmed `NormalizeUrl` char-overload fix remains unchanged;
- the 2.2.9 Kawaii DocFX/API/PDF viewer code remains intact;
- `Directory.Build.targets` and edited project files remain well-formed XML.

The release script now suppresses the automatic Pages target during its intentional assembly-only build, generates and validates the complete DocFX/PDF payload, then seeds the matching GitHub Pages ZIP explicitly from that completed payload.
