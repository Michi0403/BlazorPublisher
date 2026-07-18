# PublisherStudio v1.0.25

- Fixed a circuit-terminating `DxRichEdit.OnMailMergeSettingsChangedAsync` null-reference failure that could occur when opening a text frame created by dropping a Markdown or text file.
- New dropped Markdown and text files are converted directly into editable OpenXML/DOCX story content while retaining the existing HTML canvas preview.
- Added native Markdown-to-OpenXML conversion for headings, paragraphs, bullet items, bold, italic, and inline-code runs.
- Legacy HTML stories remain supported, but conversion now begins from the RichEdit `DocumentLoaded` event instead of racing the component from `OnAfterRenderAsync`.
- Mail-merge settings are represented by one stable `RenderFragment` and attached only after `DocumentLoaded` confirms that the RichEdit document exists.
- Closing the editor, reconnecting, or cancelling a pending legacy conversion no longer leaves an asynchronous story-editor operation capable of terminating the circuit.
- Retained `using Microsoft.AspNetCore.Components.Server;` and the existing unlimited chunked interop timeout configuration.
- Publication format marker updated to `1.25`; older publications remain loadable.
