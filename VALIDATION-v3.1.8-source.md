# PublisherStudio 3.1.8 source validation

Static validation for this source handoff covers host-aware release selection, macOS executable-mode preparation, UTF-8 `Info.plist`, optional Linux RPM/AppImage behavior, LocalGPT-owned packaging consumption, PublisherStudio architecture/component/service/interop policies, XML documentation, structured-file parsing, archive safety, and exact extracted-ZIP equality.

This environment does not contain the .NET SDK or PowerShell, so it does not claim a local `dotnet build`, PowerShell execution, DMG creation, or installer execution.
