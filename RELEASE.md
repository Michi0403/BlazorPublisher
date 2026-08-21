# PublisherStudio 2.9.4

PublisherStudio 2.9.4 is the **DevExtreme Runtime-Key Provenance Repair** release.

It retains the 2.9.3 Panel Studio behavior/object-interface enhancements and corrects the DevExtreme preparation model: one exact `devextreme@25.2.9` package now owns both runtime-key generation and the authoritative browser-runtime overlay, stale `devextreme-dist` internal metadata is no longer treated as a fatal version source, and the generated non-modular key is loaded immediately after `dx.all.js`.

Target framework remains .NET 10 (`net10.0`) and DevExpress/DevExtreme remains 25.2.9.

This archive is **SOURCE-NOT-COMPILED**. Run `Prepare-DevExpressAssets.cmd` on the licensed Windows developer/build machine after extraction, then perform the normal .NET build there.

See `CHANGELOG-v2.9.4-DEVEXTREME-RUNTIME-KEY-PROVENANCE-REPAIR.md` and `VALIDATION-v2.9.4-source.md`.
