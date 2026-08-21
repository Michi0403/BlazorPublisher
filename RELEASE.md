# PublisherStudio 2.9.3

PublisherStudio 2.9.3 is the **Panel Behaviors & DevExtreme Asset Repair** release.

Panel Studio now turns its existing interactive components into an easier publication object interface: stable object addresses, compact common behavior controls, right-click quick actions, target/method selection, and JavaScript helper snippets are persisted with the publication and used by the shared browser runtime.

The DevExtreme preparation/export path is also corrected for the 25.2.9 upgrade. Generated vendor targets are cleared/replaced with Windows retry handling, restored/copied package metadata and hashes are verified, browser URLs are cache-busted, export bypasses stale HTTP cache, and version validation uses package/manifest/license metadata instead of a brittle regex over minified `dx.all.js`.

Toolchain state remains `.NET 10` / `net10.0` with DevExpress/DevExtreme 25.2.9. Generated commercial vendor assets and private licensing material are deliberately absent from this source archive and are prepared on the licensed build machine.

This archive is **SOURCE-NOT-COMPILED**. See `CHANGELOG-v2.9.3-PANEL-BEHAVIORS-DEVEXTREME-ASSET-REPAIR.md` and `VALIDATION-v2.9.3-source.md`.
