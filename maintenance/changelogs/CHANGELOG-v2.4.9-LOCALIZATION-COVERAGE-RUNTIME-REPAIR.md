# PublisherStudio 2.4.9 — Localization coverage/runtime repair

## English/German localization coverage

- PublisherStudio's browser localization runtime now aligns the selected catalog with the maintained English catalog and builds exact source-text translations from all matching catalog keys, rather than relying only on `Text.*` keys.
- Localization now walks ordinary UI text nodes throughout the application with a `TreeWalker` and observes later Blazor/DevExpress DOM changes, including character-data and common UI attributes. Publication/user-content surfaces remain excluded so localization does not rewrite authored document content.
- The English/German maintained catalogs now contain 3,035 matching UI keys.
- Recent Editor, Panel/Div Studio, media, streaming, menu, connector, OpenSCAD and profile controls/help text that bypassed the catalog have been added to English/German coverage.
- Exact translations were added for recent commands such as operator sending, one-time approval, connect/remove actions, EBU normalization, request timeout, metadata/profile controls and additional editor guidance.
- The localization integrity gate now requires at least 3,000 aligned English/German keys and verifies the English-source mapping, TreeWalker and character-data observer runtime contract.

## Deployment/runtime

- `localizationRuntime.js` receives a `2.4.9` cache key so installed clients do not keep the older localization runtime after upgrade.
- Existing 2.4.8 Panel Studio interop and capture-lifecycle repairs remain intact; their browser module cache keys advance with the product version where applicable.

## Versions

- PublisherStudio.Web: `2.4.9`
- PublisherStudio.InstallerConsole: `2.4.9`
