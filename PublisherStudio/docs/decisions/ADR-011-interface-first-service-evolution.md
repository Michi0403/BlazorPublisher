# ADR-011: Interface-first service evolution

**Status:** Accepted

Reusable application behavior is exposed through public interfaces and registered with explicit DI lifetimes. Existing concrete injection remains compatible while touched areas migrate incrementally. MVC-specific behavior is implemented by controller-layer adapters. Static methods are restricted to composition extensions, framework entry points, generated regex and irreducible language helpers.
