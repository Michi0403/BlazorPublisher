# PublisherStudio v1.0.28

- Fixed the remaining Story Editor horizontal-placement error. RichEdit's exported HTML BODY can contain a fixed preview width, `max-width`, and automatic centering that are not DOCX page settings; those preview-only values are now removed before pagination.
- Story content is now laid out inside explicit physical page sheets using the live DOCX section width, height, orientation, top/right/bottom/left margins, and gutter. The same document margins are repeated on every page.
- Replaced browser HTML printing in the Story workflow with an application-generated PDF preview. The preview window now opens the completed PDF in the browser's native PDF viewer, so printing and Save as PDF use exactly the displayed pages.
- Removed browser-injected print decorations from Story output. Date/time, publication title, blob URL, and automatic page counters are no longer part of the generated PDF and therefore cannot appear in its print result.
- Full-page document background colors and paragraph/text fills are rasterized into each physical PDF page, including the margin region.
- Exact PDF page geometry was verified for A4 portrait, A4 landscape, and custom 148 x 210 mm documents, plus a five-page pagination test.
- Story PDFs are visual/raster PDFs to guarantee preview-to-print fidelity; text in this specific Story print output is not selectable or searchable. DOCX export remains the editable, text-preserving output.
- Publication format marker updated to `1.28`; older publications remain loadable.
