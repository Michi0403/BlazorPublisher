# PublisherStudio 3.0.7 — DocFX Language Preflight Repair

## Fixed

- Keeps the complete PublisherStudio documentation and PDF build in the normal release pipeline.
- Moves the generated-HTML `lang="en"` repair out of the website-theme injector and into a dedicated post-generation repair immediately before the existing accessibility/link preflight.
- Repairs only generated HTML documents whose opening `<html>` tag has no `lang` attribute; existing language metadata is left unchanged.
- Keeps the strict accessibility validator unchanged and preserves all application, UI, render-mode, deployment, DevExpress, and native-device behavior from 3.0.6.

## Version

- PublisherStudio web application and installer console: `3.0.7`.
- Browser asset/cache identity and npm package identity: `3.0.7`.
- LocalGPT wire protocol remains `2.1.1`.
- Minor and patch version slots remain single-digit.
