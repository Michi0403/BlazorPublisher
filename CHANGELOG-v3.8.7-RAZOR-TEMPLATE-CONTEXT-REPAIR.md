# PublisherStudio 3.8.7 — Razor template-context repair

- Explicitly names every maintained `DxFormLayoutItem` template context so nested DevExpress templates (`DxButton`, `DxPopup`, `DxComboBox.ItemDisplayTemplate`, and nested FormLayout items) cannot collide on Razor's implicit `context` parameter (`RZ9999`).
- Preserves the FormLayout-first component architecture and the containment-only root div; no DevExpress control or editor workflow was removed.
- Extends the Razor maintenance architecture guard with build-breaking `RAZORUI0010/RAZORUI0011` checks for missing/reused FormLayout template context names.
- Retains DevExpress/DevExtreme 25.2.10, component logging/resilience, text-service ownership, localization, and XML-documentation contracts.
