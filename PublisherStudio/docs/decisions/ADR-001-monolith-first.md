# ADR-001: Monolith first

**Status:** Accepted

PublisherStudio remains an Interactive Blazor Server desktop/local-network monolith. A separate process is introduced only for a real deployment, dependency, crash-isolation or scaling boundary. Automation alone is not sufficient reason to split a capability out of the monolith.
