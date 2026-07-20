# Third-party notices

PublisherStudio uses the following third-party components. Their own license terms continue to apply.

## DevExpress

- **DevExpress Blazor 25.2.8** and **DevExpress Blazor RichEdit 25.2.8** — commercial DevExpress components.
- **DevExpress ASP.NET Core Spreadsheet 25.2.8**, **DevExtreme 25.2.8**, and the Spreadsheet browser package — commercial DevExpress components and browser resources.
- **DevExtreme predefined VectorMap data** (`world.js`, `africa.js`, `canada.js`, `eurasia.js`, `europe.js`, and `usa.js`) — geographic data supplied inside the licensed DevExtreme browser package. DevExpress documents these maps as converted from a free map-data provider; the DevExtreme distribution terms still apply to the packaged scripts.

A valid DevExpress license and configured licensed NuGet/npm feeds are required to restore and build these components. The official `devextreme-license` tool generates a public/runtime key from the licensed build identity. The private DevExpress license and `node_modules` are not redistributed in the source ZIP. A licensed build copies the required browser files and generated public runtime key into the published application's local `wwwroot/vendor` directory for offline runtime use and self-contained HTML export.

## Browser-side open-source libraries

- **jQuery 3.7.1** — MIT License. Required by the DevExpress ASP.NET Core Spreadsheet integration.
- **html2canvas 1.4.1** — MIT License. Used to rasterize publication pages for PNG/JPEG export.
- **JsBarcode 3.12.1** — MIT License. Used for common linear barcode formats.
- **qrcode-generator 1.4.4** — MIT License. Used for QR Code generation.

The license or notice files for directly vendored open-source scripts are stored beside those scripts where applicable. The surrounding PublisherStudio source retains its own project license.
