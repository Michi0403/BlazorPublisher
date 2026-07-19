# PublisherStudio v1.0.27

- Story Editor print preview now reads the live DOCX section page setup instead of printing into a browser-default sheet. The OpenXML page width, height, portrait/landscape orientation, top/right/bottom/left margins, and gutter are converted from twips to physical millimetres.
- Generated story print HTML now declares the document dimensions through CSS `@page`, so Chrome/Edge print preview, physical printing, and Save as PDF select the same orientation and content area as the DOCX opened in Word or LibreOffice.
- The independent Story print-preview window renders a physical page shell with the extracted paper size and margins before the print dialog opens. Content therefore starts at the same document-margin position rather than at the paper edge.
- Page background colors continue through the complete paper area, including the margin region, while paragraph shading and text highlights retain the v1.0.26 print-fill materialization.
- Standalone HTML downloads from Story Editor use the same DOCX page setup, so their browser view and later printing no longer fall back to a generic 1200-pixel document.
- Invalid or incomplete section metadata falls back safely to A4 portrait with standard margins; extreme page sizes and impossible margin pairs are bounded before CSS is generated.
- Publication format marker updated to `1.27`; older publications remain loadable.
