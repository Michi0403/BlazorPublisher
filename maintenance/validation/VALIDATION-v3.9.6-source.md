# PublisherStudio 3.9.6 source validation

## Scope

Source-only architecture-gate repair and static validation. No .NET compilation, MSBuild, restore, publish, GitHub access, or online repository access was performed.

## Async-boundary gate

`build/audit_async_only_architecture.py` was executed directly against `src/PublisherStudio.Web` after correcting the 3.9.5 scanner.

Observed result: **pass**.

- 228 maintained Component/Service/HostedService source files reviewed.
- 0 async-boundary findings.
- No application-source fire-and-forget rewrite was required in PublisherStudio: its 3.9.5 `ASYNC011` inventory was scanner noise caused by `_ => await ...` lambda syntax rather than real `_ = ...` discarded tasks.

The shared synthetic positive fixture correctly produced seven failures for invalid async contracts/ownership patterns. The synthetic negative fixture passed the false-positive cases that motivated the repair: pure synchronous helpers, framework-style callbacks, domain `.Result` values, `_ => ...` lambdas, `_ = await ...`, bounded atomic state, and synchronous-only disposal.

## Repository static guards

The following source audits were executed directly with Python and passed:

- application architecture (`audit_application_architecture.py --mode all`);
- async continuation policy: 82 source files, 1,108 await tokens, 433 `ConfigureAwait(false)`, 621 renderer-affine `ConfigureAwait(true)`, 49 configured await-using disposals, and 5 configured async streams;
- Razor maintenance architecture: 48 maintained Razor components.

Python syntax validation was performed for the repaired async-boundary audit. PublisherStudio 3.9.6 project/package/cache-busting version references were checked statically for consistency.

## Behavioral boundary of this handoff

PublisherStudio application behavior was not intentionally refactored for this release. The repair is limited to the broken maintenance rule, its documentation, and the normal release/cache-busting version increment. DevExpress component ownership, render modes, Panel Studio geometry, native/streaming adapters, publishing/installer workflows, and persisted formats are unchanged.
