# PublisherStudio 3.1.2 source validation

This release is validated as source because the requested environment intentionally does not use GitHub or a .NET SDK/build.

The delivery gate is run against a fresh extraction of the exact source ZIP and requires all of the following to pass:

- `build/audit_release_3_1_2.py`
- `build/audit_application_architecture.py --root <root> --product publisherstudio --mode all`
- `build/audit_async_continuations.py --source-root <root>/src/PublisherStudio.Web`
- `build/audit_service_resilience.py --root <root> --product publisherstudio`
- `build/audit_cross_platform_boundaries.py`
- `build/audit_prerender_interop_safety.py --root <root>`
- `build/audit_panelstudio_persistence.py`
- `build/audit_component_resilience.py --root <root>`

The release audit verifies version identity and one-digit slot policy, persisted permissive replay capacity, repository-owned packaging tool wiring, all seven application RIDs, Windows-only setup publishing, native package-format paths, checksums, and the reviewed Interactive Server page boundaries.

No compiler-clean or native-package-build claim is made without a real .NET/macOS/Linux toolchain run.
