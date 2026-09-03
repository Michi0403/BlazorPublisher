# Third-party notices

PublisherStudio uses the following third-party components. Their own license terms continue to apply.

## DevExpress

- **DevExpress Blazor 25.2.9** and **DevExpress Blazor RichEdit 25.2.9** — commercial DevExpress components.
- **DevExpress ASP.NET Core Spreadsheet 25.2.9**, **DevExtreme 25.2.9**, and the Spreadsheet browser package — commercial DevExpress components and browser resources.
- **DevExtreme predefined VectorMap data** (`world.js`, `africa.js`, `canada.js`, `eurasia.js`, `europe.js`, and `usa.js`) — geographic data supplied inside the licensed DevExtreme browser package. DevExpress documents these maps as converted from a free map-data provider; the DevExtreme distribution terms still apply to the packaged scripts.

An appropriately licensed DevExpress development environment is required for the maintained DevExpress-based build. The current repository restores .NET packages from NuGet.org (and the LocalGPT wire-protocol package from its explicit local cache); browser packages are restored through npm according to `package-lock.json`. The official DevExpress license-generation tooling creates the public/runtime key from the licensed build identity. The private DevExpress developer license and `node_modules` are not redistributed in the source ZIP or end-user installation. A licensed build copies the required redistributable browser files and generated public runtime key into the published application's local `wwwroot/vendor` directory for offline runtime use and self-contained HTML export, subject to DevExpress's own terms.

## Browser-side open-source libraries

- **jQuery 3.7.1** — MIT License. Required by the DevExpress ASP.NET Core Spreadsheet integration.
- **html2canvas 1.4.1** — MIT License. Used to rasterize publication pages for PNG/JPEG export.
- **JsBarcode 3.12.1** — MIT License. Used for common linear barcode formats.
- **qrcode-generator 1.4.4** — MIT License. Used for QR Code generation.

The license or notice files for directly vendored open-source scripts are stored beside those scripts where applicable. The surrounding PublisherStudio source retains its own project license.

## Optional external FFmpeg executable

PublisherStudio can invoke a separately installed FFmpeg executable for local media conversion. FFmpeg is **not bundled or redistributed** with PublisherStudio. FFmpeg is normally licensed under LGPL 2.1-or-later, while optional GPL components can make a particular build GPL; codec and patent obligations also depend on the selected build and distribution. The user or distributor is responsible for installing and licensing an appropriate FFmpeg build. See the official FFmpeg legal and download pages.
