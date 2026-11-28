# PublisherStudio 4.6.0 — live browser automation visibility

- Preserved the existing per-tab opt-in browser automation queue and LocalGPT 1-Wire permission/trust boundaries.
- Mouse/Pointer down, move and up now maintain pressed-button state for canvas drags and display a visible automation pointer in the browser tab.
- Transport, pending command, completion and screenshot errors reach the existing `OperationalNotifications` error popup via JavaScript diagnostics and the InteractiveServer bridge.
- A bounded pre-connection error queue replays transport errors when the Blazor diagnostics bridge attaches; poll retries remain active through prerender state.
- Existing `publisher.input.execute`, view inspection, screenshot capture and result capabilities remain authoritative; this is not a new autonomous browser agent.

See `VALIDATION-4.6.0-OFFLINE.md`. Native OS input and full PublisherStudio runtime/circuit paths were not exercised.
