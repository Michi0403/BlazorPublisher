# PublisherStudio 4.7.7 — fresh 1-Wire Council directory and truthful errors

## Defect evidence

On a paired MacBook Neo, PublisherStudio's AI-enabled components received completed 1-Wire `WorkResult` envelopes containing LocalGPT execution failures. The bridge tried to extract `ResultJson` and emitted the misleading message `LocalGPT returned an empty Council result`. Model and Council team availability can change while the 1-Wire link stays authenticated.

## Changes

- `PublisherAiBridgeService.ChatAsync` sends an authenticated `CapabilityRequest` and awaits the correlated `CapabilityResponse` before dispatching a Council chat request, confirming `council.run` is still enabled and online. The acknowledged response payload is authoritative; a cached discovery advertisement is not proof of current availability.
- Failed/cancelled `WorkResult` status and the returned LocalGPT error are handled before reading `ResultJson`. No successful Council answer is fabricated on a failed execution.
- The existing explicit team key/model selections, approval boundary, and 1-Wire MFA protocol are preserved; no new model assignment, pairing flow, or transport format is introduced.
- DevExpress Council team selector components in `Editor.razor` have `InputId` attributes matching their associated HTML `<label for>` identifiers. This repairs the reported accessibility/autofill warning for these two controls.

## Boundaries

Tab automation and Media Studio screen recording are separate browser APIs. The recorded tab-automation activation and HTTP polling were successful, but completed tab actions were not proven by these logs. This revision does **not** claim a tab-command-execution fix or a browser permission fix.

## Release identity

PublisherStudio application and installer source versions are 4.7.7; installer behavior was not changed.
