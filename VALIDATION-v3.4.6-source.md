# PublisherStudio 3.4.6 source validation

Static/source validation checks:

- active version-bearing files use 3.4.6;
- common `FfmpegLocator` still owns scope-neutral precedence and contains no new OS branching;
- Unix platform service covers system-wide, user-wide, Linuxbrew/Nix and inherited-PATH resolution;
- InstallerConsole FFmpeg discovery mirrors user/system Unix candidate paths;
- custom configuration/environment and portable paths remain supported;
- 3.4.5 documentation-browser fail-fast/chunking repair is preserved;
- artifact-local notarization logic is preserved;
- application architecture, cross-platform, async, component, prerender, persistence and render-mode audits remain green.

This environment does not contain `dotnet`, PowerShell, macOS Finder, FFmpeg, Xcode signing tools or Apple's notary service. A real target-host runtime smoke test remains required before claiming end-to-end execution.
