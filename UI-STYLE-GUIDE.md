# PublisherStudio UI Style Guide

PublisherStudio keeps DevExpress/DevExtreme components as behavioral owners while preserving the established Office/Bootstrap visual language. UI repairs should be local and compositional rather than broad theme overrides.

## Control contrast and states

- When a DevExpress semantic class remains on a locally light surface, explicitly own foreground, background, border, hover/focus, selected and disabled states at that local surface.
- Prefer Bootstrap/DevExpress-compatible variables such as `--bs-body-color`, `--bs-body-bg`, `--bs-border-color` and `--bs-primary`, with bounded fallbacks.
- Child captions, descriptions and icons must remain readable when a host control overrides a DevExpress background. Do not globally recolor DevExpress primary controls to repair one studio.

## Form and checkbox alignment

- Ordinary field labels may use a vertical grid with caption above editor.
- Boolean values are one horizontal unit: checkbox followed by caption/value on the same row. Generic label-grid rules must not override `.check-row` or `.publisher-check-row`.
- Preserve keyboard focus visibility and consistent field baselines.

## Modal geometry

- Large studios may use viewport-sized work areas. Configuration/export dialogs use a fixed responsive footprint (`min(fixed-size, viewport-size)`) rather than a near-full-screen shell around a small child panel.
- A large `DxPopup` whose descendants use percentage heights must provide a definite responsive `Height` as well as a viewport-bounded `MaxHeight`; percentage-height Studio roots must resolve against the DevExpress popup body, not the browser or a removed legacy backdrop.
- The DevExpress modal dialog stays inside the current viewport. Its modal body is the fallback overflow owner (`min-width/min-height: 0`, bounded size, internal scrolling), so oversized controls remain reachable without painting beyond the popup border.
- Dropdown/listbox popup portals keep their own DevExpress overlay and scrolling behavior; do not apply modal-body overflow rules to those cells.
- The dialog surface fills or fits the popup body. Long content scrolls inside the popup/content region while header/actions remain reachable.
- Avoid nested geometry owners that fight DevExpress popup measurement. `BodyContentTemplate` exposes the real dialog/studio surface directly; a maintenance `DxFormLayout` must not sit between the DevExpress modal body and that surface.
- Inactive popup/studio components do not leave empty DevExpress FormLayout item trees in the editor DOM. Gate their complete rendered boundary on the same visibility/readiness condition as the popup.
- Page appearance/effects is a responsive configuration window, not a full-screen workbench; its body owns scrolling while its header and footer remain reachable.

## Publication geometry and export parity

- Inspector-visible geometry properties must affect Mainframe, panel rendering and print/export consistently. Shape corner radius applies to rectangular shapes as well as the rounded-rectangle preset; ellipse remains `50%`.
- Presentation exports show playback controls by default unless the document explicitly disables them. Controls render above publication content and are hidden for print.

## Maintenance wrappers

- Required `DxFormLayout` maintenance shells remain box-neutral (`display: contents`) where they do not own visual geometry. Mainframe, rulers, panes, editors and dialogs retain their established geometry owners.
- Do not remove InteractiveServer render-mode boundaries to solve a styling problem.

## Browser lifecycle diagnostics

- Browser diagnostics observe failures through `window.error`, `unhandledrejection`, the .NET diagnostics bridge and explicit PublisherStudio guard helpers.
- Diagnostics must not replace `EventTarget` listener methods, timers, animation-frame scheduling, microtasks or observer constructors. DevExpress owns those browser lifecycle primitives; changing callback identity or slot timing is itself a frontend regression risk.
