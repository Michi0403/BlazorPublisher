# PublisherStudio 3.4.7 source validation

Static/source validation checks the per-user application-data contract, Windows `%LOCALAPPDATA%\PublisherStudio` compatibility, macOS Application Support fallback, Linux `XDG_DATA_HOME`/`~/.local/share` behavior, first-boot path reporting, user configuration precedence, Publisher content defaults, FFmpeg/tool discovery separation, existing browser-chunked documentation repair, artifact-local notarization behavior, PowerShell compatibility and InteractiveServer architecture.

This environment does not provide the full .NET runtime/toolchain, macOS Finder, FFmpeg, Xcode signing tools or Apple's notary service. Real target-host smoke tests remain required before claiming end-to-end execution.
