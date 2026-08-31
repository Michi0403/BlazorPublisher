# PublisherStudio documentation

**Version 3.2.1**

PublisherStudio is a local-first publishing workspace for page layout, stories, spreadsheets, pictures, audio, video, streaming, interactive content, and self-contained exports.

This site is the maintained product and architecture documentation. It uses the same documentation shell as LocalGPT, so navigation, search, themes, side rails, and generated API pages behave consistently. 🐾

## Choose a path

### 🌸 Use PublisherStudio

Create a publication, learn the workspace, and understand pages, objects, stories, ribbons, and export workflows.

[Open the user guide](articles/getting-started.md)

### 🎨 Build rich content

Work with stories, spreadsheets, pictures, audio, video, animation, interaction, streaming, and recording.

[Explore pictures and media](articles/pictures-and-media.md)

### 🛠️ Build and maintain it

Read the modular architecture, development requirements, installer behavior, documentation pipeline, and release checks.

[Open engineering guidance](articles/developer-build.md)

### 📚 Look up details

Browse publishing and export behavior, privacy boundaries, the optional LocalGPT connection, and the generated API reference.

[Browse reference material](articles/documentation-system.md)

## Architecture at a glance

```
flowchart LR
    U[Human author] --> UI[Blazor + DevExpress workspace]
    UI --> APP[Application services]
    APP --> DOC[Publication model]
    APP --> MEDIA[Media and streaming services]
    APP --> EXPORT[Print, PDF, web and recording exports]
    APP --> WIRE[Optional LocalGPT 1-Wire boundary]
    DOC --> STORE[(Local project data)]
    MEDIA --> DEVICE[Approved local devices]
    WIRE --> PEER[Approved LocalGPT peer]
```

The editable publication remains authoritative. Browser runtimes accelerate interaction and rendering, while C# services own project state, validation, persistence, export, and approved external connections.

## Complete documentation set

The conceptual pages are built together with compiler-generated XML documentation for public types and members. The same themed HTML site is shipped inside PublisherStudio and published to GitHub Pages.

The packaged PDF is built from the same reviewed Kawaii HTML tree as the website. It contains every maintained PublisherStudio chapter and every generated API namespace/type page; a tiny source-only or fallback PDF is rejected by the release and Pages gates.

[🐾 Download the Kawaii handbook](~/PublisherStudio-3.2.1.pdf)
