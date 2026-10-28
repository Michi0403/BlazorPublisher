# PublisherStudio 4.7.0 — LocalGPT Connection Entry Repair

## Corrected

- The Home ribbon control labelled **Connect LocalGPT** (and the intermediate Approve LocalGPT link state) now activates PublisherStudio's existing organic-plugin/1-Wire connection workflow.
- Previously, the same control invoked the AI authoring callback; `Editor.OpenSelectedAiAssistAsync` returned silently when not already connected and linked. This produced the exact no-action symptom on macOS.
- Once the link is connected and mutually approved, the control continues to open the existing context-appropriate native AI authoring surface.

## Unchanged

- The 4.6.9 PowerShell service-architecture fix, and all other pre-existing 1-Wire security/authorization gates.
- Visibility of AI controls remains linked to an approved active connection; no AI-only command is shown to unrelated offline users.
- MFA QR/dialog implementation is intentionally unchanged as requested.

## Validation limits

No .NET/DevExpress browser runtime or PowerShell architecture-validation environment was available. This is a minimal source-level correction to a confirmed call-chain mismatch, not a declaration of verified end-to-end pairing.
