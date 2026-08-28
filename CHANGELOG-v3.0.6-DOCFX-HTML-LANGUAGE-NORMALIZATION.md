# PublisherStudio 3.0.6 — DocFX HTML Language Normalization

## Fixed

- Normalizes generated DocFX HTML documents to a non-empty `lang="en"` attribute during the existing website-theme injection pass.
- Fixes the real 3.0.5 release failure where DocFX completed successfully but the pre-PDF accessibility validator rejected generated API pages for `missing html lang`.
- Keeps accessibility/link validation strict; generated output is repaired before validation instead of suppressing or bypassing the rule.
- Preserves the 3.0.5 native-device discovery hardening, DevExpress/Node preparation, and existing InteractiveServer topology.

## Version

- PublisherStudio web application and installer console: `3.0.6`.
- Browser asset/cache identity and npm package identity: `3.0.6`.
- LocalGPT wire protocol remains `2.1.1`.
- Minor and patch version slots remain single-digit.
