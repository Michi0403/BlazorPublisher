# Interface-first API evolution — v1.0.85

## Goal

PublisherStudio remains one Interactive Blazor Server application, but reusable work must no longer be trapped in private static helpers or frontend-only code. Components, controllers, hosted services, LocalGPT, AICouncil and future plugins should call the same application services.

## Rules

1. Reusable behavior is declared through a public interface and implemented by a service under `Services`.
2. Every registration has an intentional lifetime. Stateless catalogs, formatters and adapters are normally singleton; editor/session state is scoped; short-lived operations may be transient.
3. Controllers are thin HTTP adapters. They bind requests, call the same service used by components, and return domain/result contracts.
4. Services never depend on MVC, controllers, components, hubs or hosted services. MVC-specific reflection is isolated in `Controllers/ApiSurfaceCatalogService.cs` behind `IApiSurfaceCatalogService`.
5. Static methods remain acceptable only for framework entry points, extension methods, constants/compiler-generated regex and genuinely language-level helpers that have no injectable behavior.
6. Existing public APIs and component injection remain compatible while interfaces are introduced beside them. Migration is incremental, not a rewrite.

## Composition root

`PublisherStudioServiceCollectionExtensions.AddPublisherStudioApplication` owns application registrations and records `ServiceArchitectureDescriptor` entries. The final descriptor scan also includes application-owned legacy concrete registrations without changing their existing call sites. `/api/domain-context` exposes service contracts, lifetime, public methods, related domain objects and controller routes for local automation clients.

## New API surfaces

- `/api/automation/input`
- `/api/automation/screenshots`
- `/api/domain-context`
- `/api/openscad`
- `/api/video-layer-interchange`
- `/api/code`
- `/api/configuration`
- `/api/render-export`

These routes are loopback-hosted by the existing PublisherStudio process. They do not create a second backend architecture.

## Incremental migration policy

Legacy concrete services and static helpers are not mass-rewritten. When a legacy area is touched, reusable logic is extracted behind an interface, the old call path stays compatible, and an architecture test is added. The current task ledger identifies remaining candidates.
