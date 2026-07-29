# ADR-002: Controllers and Hubs are backend entry points

**Status:** Accepted

Normal main-host HTTP and WebSocket request routes begin in the existing `Controllers` structure. Persistent connection entry roles live in `Hubs`. Minimal-API endpoint aggregations are not used as an alternative application architecture.

There is no separate `Backend` root. Controllers and Hubs delegate reusable processing and technical I/O to Services. A Service-owned private protocol listener may expose only its isolated transport routes and must not become a second business routing architecture.
