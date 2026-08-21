# PublisherStudio 2.9.2

PublisherStudio 2.9.2 is the **Translation Editor Localization** release built forward from 2.9.1. It preserves the existing editor/runtime architecture and Panel Studio persistence behavior.

## Toolchain state retained

- SDK policy: `10.0.301` with `latestFeature` roll-forward
- Target framework: `net10.0`
- DevExpress: `25.2.9`
- LocalGPT 1-Wire protocol: `2.1.1`

## Main changes

The Translation Editor now uses PublisherStudio's existing file-localization service for its own page title, labels, actions, status and notifications. Culture choices show localized culture display names instead of raw culture codes. Matching editor keys are present across the six maintained catalogs. Browser module cache identifiers were advanced with the application version.

## Validation boundary

This source package was not compiled with .NET/DevExpress in the preparation environment. Validation is source/static only. See `CHANGELOG-v2.9.2-TRANSLATION-EDITOR-LOCALIZATION.md` and `VALIDATION-v2.9.2-source.md`.
