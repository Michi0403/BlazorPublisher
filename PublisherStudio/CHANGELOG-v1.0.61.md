# PublisherStudio 1.0.61

## Stable and exact mainframe zoom control

- The status-bar zoom slider now works in integer percentage units instead of mixing a fractional range with multiplicative `1.15` button steps.
- Dragging the slider no longer commits a Blazor state update on every pointer movement. The native thumb moves locally and the canvas zoom is committed once when the gesture finishes, preventing the rerendered canvas and changing scroll geometry from pulling the thumb away from the pointer.
- The slider uses deterministic whole-percentage positions from 20% through 400%. An adjacent numeric field accepts any exact whole percentage, and a dedicated **100%** button resets the canvas immediately.
- Plus and minus now move by exact 5% increments. They no longer create fractional percentage values such as 132.25% or 152.0875%, which an integer-percentage control cannot represent consistently.

## Why both rendering modes can remain sharp

- Both modes render live DOM, SVG, RichEdit HTML, and vector glyphs from the same stable 96-DPI authored content wrapper; neither mode enlarges a captured bitmap.
- **Sharp CSS layout** participates in layout and rasterization at the requested zoom. **Compact transform** lays out at authored size and composites the wrapper afterward, but Chromium/Edge can still rerasterize live text at the current device pixel ratio. On UHD displays this can make both modes look very sharp, with the remaining small difference coming from layout zoom versus compositor scaling.
- Export and print rendering remain unchanged.

Application and installer version: `1.0.61`. Publication format remains `1.47`; picture format remains `1.2`.
