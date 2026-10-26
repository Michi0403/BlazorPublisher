# PublisherStudio 4.6.3 — Council form build-gate repair

- Fixes the new Council form's `Assert-ComponentSafety.ps1` violation. `OrganicCouncilRequestForm` now implements task-returning `OnParametersSetAsync()` and preserves the original request-object identity check, draft population, exception logging, and parent lifecycle call.
- Fixes the new Council BusinessObject's `Assert-IteratorExceptionPolicy.ps1` violation. `OrganicCouncilRequestFormModel.Validate` now returns a materialized list of member-bound DataAnnotations errors, retaining the existing prompt, team, and JSON checks without producing an iterator that wraps `yield` with `catch`.
- Adds three regression cases checking both build-blocking architecture rules, preserved transport callbacks and notifications, and model-member validation.
- Bumps Web and InstallerConsole project versions to 4.6.3. Both existing PowerShell guards and central DI wiring are unchanged.

No unrelated form or Council runtime processing changes.

See `VALIDATION-4.6.3-OFFLINE.md` for the validation boundary.
