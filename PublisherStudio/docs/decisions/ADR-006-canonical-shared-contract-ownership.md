# ADR-006: Canonical shared-contract ownership

**Status:** Accepted

PublisherStudio uses one authoritative type for each semantic request, event, state or result that crosses Components, Controllers, Hubs, Services or HostedServices. The existing `Domain` or `Models` area owns those shared contracts. In-process facades and Services consume the canonical type directly and must not declare same-named compatibility or mapping copies.

A provider-, protocol- or process-specific DTO exists only at a real external boundary. Its name must identify the boundary or role, such as `Request`, `Response`, `Dto` or `Message`; it is converted once and does not become a second application contract.

`GlobalUsings*.cs` contributes to one compilation-wide simple-name scope. Automated architecture checks reject collisions between public/internal types visible from globally imported namespaces and reject Services shadowing Domain/Models contracts. Contract moves remove the former declaration in the same change.

This decision prevents ambiguous references such as the duplicate `MediaHostHotkeyEvent` introduced during the v1.0.63 streaming reorganization.
