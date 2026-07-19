# PublisherStudio v1.0.26

- Replaced Story Editor's hidden `about:blank` print iframe with an independent blob-backed print-preview window. The window is reserved synchronously from the original browser click or Ctrl+P key event, then populated after RichEdit exports finish, so popup blocking and asynchronous editor export do not race each other.
- Routed both PublisherStudio's Story Export > Print command and DevExpress RichEdit's built-in File > Print command through the same color-preserving preview pipeline. The built-in command is intercepted before it can start its background-dropping `about:blank` print path.
- Story print preview now materializes document-page fills, paragraph shading, and text highlights as print-safe inline fills before invoking the browser print dialog. The preview stays available with explicit Print and Close controls after the first dialog closes.
- Leaving the Story Editor print preview open no longer keeps an iframe or callback attached to the Blazor circuit. Applying or cancelling the story therefore cannot be blocked by an unfinished print-preview lifecycle.
- Added desktop drag/drop and clipboard-file acceptance for `.docx` Office Open XML documents. The original DOCX bytes become the text frame's native story content and open directly in DevExpress RichEdit.
- Added a lightweight OpenXML canvas preview for imported DOCX paragraphs, headings, lists, tables, run fonts, sizes, emphasis, foreground colors, highlights, page color, and embedded-picture placeholders. RichEdit remains the full-fidelity editor and source of truth.
- Invalid or renamed non-DOCX ZIP files are rejected before a text frame is created.
- Retained `using Microsoft.AspNetCore.Components.Server;`, recovery, publication printing, drag/drop media, keyboard clipboard, grouping, animations, and presentation export behavior.
- Publication format marker updated to `1.26`; older publications remain loadable.
