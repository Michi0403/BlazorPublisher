# Local automation and screenshot API

## Purpose

LocalGPT and AICouncil can coordinate the running PublisherStudio browser UI through the same loopback application process. The implementation is intentionally browser-local and does not inject global operating-system input.

## Flow

1. A client posts a `BrowserAutomationCommand` or `BrowserScreenshotRequest`.
2. A singleton queue stores the request and status.
3. `automationInterop.js` claims pending requests from the active PublisherStudio page.
4. The browser executes the DOM input or captures the selected element with the existing html2canvas runtime.
5. The browser posts completion metadata or an error.
6. The client reads status or downloads the completed screenshot.

## Supported browser input

Click, double-click, context menu, pointer move/down/up, wheel, focus, blur, text/value input and keyboard down/up/press are represented by domain objects. Selectors, coordinates, buttons and modifier keys are explicit.

## Safety and scope

- Routes are served by the loopback PublisherStudio host.
- Commands target DOM elements inside the application page.
- No Win32/Linux/macOS global input injection is included.
- Browser security still applies to cross-origin frames and tainted canvases.
- The queue is process-memory state and is cleared on restart.

## Domain context

`/api/domain-context` reports exported business objects and public properties, registered services/interfaces/lifetimes, service methods and controller routes. MVC discovery is performed by a controller-layer adapter so reusable services remain MVC-free.
