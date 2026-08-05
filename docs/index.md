# PublisherStudio documentation

Welcome to the cozy side of serious publishing. PublisherStudio brings page layout, stories, spreadsheets, pictures, audio, video, streaming, and interactive web exports into one local workspace.

<div class="publisherstudio-home-grid">
  <a class="publisherstudio-home-card" href="articles/getting-started.md"><strong>🌸 Start here</strong><span>Create a publication and learn the main workspace.</span></a>
  <a class="publisherstudio-home-card" href="articles/pictures-and-media.md"><strong>🎨 Pictures and media</strong><span>Edit pictures, video, audio, and reusable media sequences.</span></a>
  <a class="publisherstudio-home-card" href="articles/publishing-and-export.md"><strong>✨ Publish and export</strong><span>Prepare print, PDF, websites, images, and recorded presentations.</span></a>
  <a class="publisherstudio-home-card" href="articles/streaming-and-recording.md"><strong>🎥 Stream and record</strong><span>Build a local production session without giving up project ownership.</span></a>
  <a class="publisherstudio-home-card" href="articles/localgpt-and-onewire.md"><strong>🐾 LocalGPT connection</strong><span>Understand the optional 1-Wire link and its approval boundaries.</span></a>
  <a class="publisherstudio-home-card" href="api/index.md"><strong>📚 API reference</strong><span>Browse services, controllers, components, and BusinessObjects.</span></a>
</div>

## What PublisherStudio is

PublisherStudio is an Interactive Blazor Server application that runs on the local machine. The browser surface is the desktop workspace; C# services keep publication state authoritative, and browser code handles fast visual interaction where it belongs.

The default web endpoint is `http://127.0.0.1:58071`. Projects, recordings, settings, and protected connection data stay under the signed-in user's control.

## A gentle path through the guide

1. Begin with [Getting started](articles/getting-started.md).
2. Learn the [editor workspace](articles/editor-workspace.md).
3. Add [stories and spreadsheets](articles/stories-and-spreadsheets.md).
4. Shape [pictures and media](articles/pictures-and-media.md).
5. Finish with [publishing and export](articles/publishing-and-export.md).

> [!TIP]
> PublisherStudio is large, but you do not need to learn it all at once. Pick one page, one object, and one export goal. The rest can wait politely in the ribbon. 🌸

## Documentation formats

This guide is generated with DocFX from maintained Markdown and C# XML comments. The same build produces:

- this searchable HTML site;
- a versioned PDF book;
- an XML-backed API reference;
- a status manifest used by the app and GitHub Pages workflow.





