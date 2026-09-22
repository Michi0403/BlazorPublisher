# PublisherStudio 3.7.5

## Interaction release gate checklist

- [x] Canonical Mainframe/Studio object ownership is unchanged; controller input invokes existing editor/Panel Studio operations rather than creating a second content model.
- [x] Existing selection, move/nudge, duplicate, delete and layer/context operations remain owned by the existing editor systems; the new adapter does not replace keyboard or pointer paths.
- [x] Mouse, pen/touch, keyboard and controller/gamepad paths coexist. Controller **Control** and **Cursor** modes route into existing semantic actions rather than synthesizing a replacement keyboard interface.
- [x] Cursor/mode indicators are transient local UI projections and do not enter publication content or Mainframe Z-order.
- [x] Preview/export/print/video publication behavior is unchanged because controller indicators and input state are not canonical publication objects.
- [x] Existing listener/interop lifecycle ownership is retained; the controller adapter reuses the existing Publisher interop runtime and does not globally capture native pointer input.
- [x] New reusable services retain structured diagnostics; capability/controller failures remain bounded instead of tearing down publication state.
- [x] Existing notification/failure boundaries are preserved; no new expected disconnect is promoted to an alarming user error.
- [x] Static interaction, persistence, architecture, async, resilience, prerender and JavaScript-integrity regression audits pass.

## Capability and controller enhancements

- Adds `publisher.file.formats` as an explicit 1-Wire/public capability backed by a reusable service and HTTP controller.
- Advertises maintained format families in `x-publisher-format-families`, including whether direct import is available and whether a live media runtime is required/available.
- Publisher media capability now reports offline when its media runtime is actually unavailable instead of implying universal media support.
- Adds additive **Control** and **Cursor** gamepad modes. Cursor mode uses analog pointer movement, primary activation and context-menu invocation while leaving native mouse/touchpad input available for Steam Deck hybrid input.
- Existing keyboard shortcuts, context menus, pointer editing, canvas controller paths and Panel Studio controller paths remain in place.
- PageSurface and Panel Studio expose semantic controller commands so analog input no longer needs to masquerade as synthetic keyboard presses.

## Validation scope

Source-only validation is used. No GitHub access, `dotnet`, restore, NuGet, MSBuild, build, publish or installer execution is used in this environment.
