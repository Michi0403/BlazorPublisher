# PublisherStudio

PublisherStudio is a .NET 10 Blazor Server publication editor built from the useful hosting pattern of LocalGPT, but without its AI workbench, Ollama, EF database, model council, diagnostics, or WinUI requirement.

## What is implemented

- DevExpress Blazor `DxRibbon` with File, Home, Insert, Page Design, contextual Picture Tools, Text Box Tools, and Shape Tools tabs.
- A Publisher-style multi-page workspace with horizontal and vertical rulers, page thumbnails, layers, selection handles, drag, resize, rotation, snapping-friendly millimetre coordinates, ordering, alignment, duplication, undo, and redo.
- Text frames edited with the DevExpress Blazor Rich Text Editor and its built-in Office ribbon.
- Image frames with free placement, fit/fill, crop panning, rotation, flipping, opacity, brightness, contrast, saturation, hue, inversion, grayscale, sepia, blur, masks, borders, shadows, and frame-ratio presets.
- Rectangles, rounded rectangles, ellipses, and lines.
- Native JSON publication format (`.pubstudio.json`) with no third-party serialization library.
- Browser print workflow and JSON save/open. Large publication downloads use Blazor stream interop instead of sending one oversized JavaScript string.
- Optional InstallerConsole that can install a published payload, start the Blazor host, uninstall it, or download and publish a GitHub/source ZIP without requiring Git.

## Requirements

- Visual Studio with .NET 10 SDK (the included `global.json` accepts the .NET 10 feature bands starting at 10.0.100).
- DevExpress DXperience 25.2 packages and a configured DevExpress NuGet feed.
- A valid DevExpress license.

## Build and run

```powershell
dotnet restore PublisherStudio.sln
dotnet run --project src/PublisherStudio.Web
```

The web host binds to loopback. Pass an optional port:

```powershell
dotnet run --project src/PublisherStudio.Web -- --port 5198
```

When no port is supplied, Kestrel asks the operating system for a free loopback port and writes the resolved endpoint to:

```text
%LOCALAPPDATA%/PublisherStudio/runtime/server.json
```

## InstallerConsole

Publish the web project and install it:

```powershell
dotnet publish src/PublisherStudio.Web -c Release -r win-x64 --self-contained false -o artifacts/payload
dotnet run --project src/PublisherStudio.InstallerConsole -- install --payload artifacts/payload --start
```

Start an existing install:

```powershell
dotnet run --project src/PublisherStudio.InstallerConsole -- start
```

Build directly from a ZIP without Git:

```powershell
dotnet run --project src/PublisherStudio.InstallerConsole -- source --source-zip https://github.com/OWNER/REPOSITORY/archive/refs/heads/main.zip --start
```

## Deliberate limits of this first foundation

This is an editor foundation, not a claim of full Publisher/InDesign parity. Text frame linking, master pages, CMYK/prepress, advanced Bézier tools, PDF/X, color management, and production-grade pagination are explicit later milestones. The architecture leaves those features isolated instead of hiding them inside one oversized page component.

## Validation

See [`VALIDATION.md`](VALIDATION.md) for completed static checks and the local compiler commands.
