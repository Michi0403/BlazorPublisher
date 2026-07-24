# ADR-002: Main application entry points use controllers

**Status:** Accepted

Main-host HTTP and WebSocket routes live in the existing `Controllers` structure. Minimal-API endpoint aggregations are not used as an alternative application architecture. Backend-owned protocol listeners may register private transport routes inside their isolated host when that route is part of the protocol implementation rather than a business/application entry point.
