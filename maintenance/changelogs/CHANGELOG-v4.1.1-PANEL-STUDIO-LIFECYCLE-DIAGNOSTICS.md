# PublisherStudio 4.1.1 — Panel Studio lifecycle diagnostics

## Summary

PublisherStudio 4.1.1 does not add another speculative Panel Studio timing fix. The repeated Windows runtime evidence shows that the DevExpress Blazor `componentContentChanged` → `initializeComponent` → null `addEventListener` exception can occur immediately after the blank preset is created, before Panel Studio reaches authored mutations. That means the remaining problem cannot be diagnosed reliably from the existing C# lifecycle log alone.

This release therefore makes the Panel Studio / DevExpress initialization lane observable on both sides of the Blazor boundary while preserving vendor ownership of browser lifecycle primitives.

## Diagnostics added

- Added an observational browser `MutationObserver` in `javascript-diagnostics.js` that records a bounded recent history of Panel Studio and DevExpress modal/popup custom-element additions, removals, branch/open state changes and `data-qa-dxbl-loaded` transitions.
- DevExpress/Panel Studio JavaScript failures now append a structured lifecycle snapshot at the moment of failure. The snapshot includes the Panel Studio source (`blank` or `existing`), binding ID, active view, preview/authoring revisions, canvas connectivity/binding state, panel-root presence, DevExpress modal/root/dialog state, loaded vs pending `dxbl-*` descendants, popup portals, active element and the recent lifecycle trace.
- The diagnostics remain observational: EventTarget, timers, animation frames, microtasks and DevExpress code are not monkey-patched.
- Added `publisherStudio.capturePanelStudioLifecycle` so the C# component can capture the same browser state at explicit lifecycle phases.
- Panel Studio now logs draft initialization with source, panel/view IDs, view/element counts, binding ID and revisions.
- Panel Studio logs a first browser snapshot before interaction binding and a second snapshot after a successful binding. Binding failures include a failure-phase browser snapshot in the C# error record.
- Added stable `data-panel-studio-*` correlation attributes on the real popup-body root so global browser diagnostics can identify the exact Panel Studio instance even before its own interaction binding is active.

## Preserved

- 4.1.0 direct popup-body topology and the supplied 3.7.6 comparison remain intact.
- 4.0.8 authored-render revision and 4.0.9 first-element refresh remain unchanged.
- 4.0.9 prepared DevExtreme asset correction and strict website export asset verification remain unchanged.
- No DevExpress vendor file is modified.
- No build/maintenance PowerShell script is modified.
- No JavaScript exception is suppressed or downgraded.

## Version

`4.1.0 -> 4.1.1`, following the maintained single-digit version-slot rule.
