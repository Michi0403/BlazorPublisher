# PublisherStudio 2.0.4 — service-owned compiler repair

This release completed the service-owned compiler migration:

- startup composition uses an explicit bootstrap logger;
- document, preset, layout, file-name, streaming, capture, chat, and LAN construction goes through injected services and factories;
- shared serializable data belongs to `PublisherStudio.BusinessObjects`;
- no application convenience statics were introduced;
- LocalGPT wire protocol compatibility remains independently versioned.

The old custom installer flow mentioned by earlier packages is superseded by the PublisherStudio 2.1.1 LocalGPT-aligned deployment contract.
