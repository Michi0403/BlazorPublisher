# Runtime value ownership

## Binding rule

Components, controllers, orchestration services, and text services do not own runtime regex text, regex options, match timeouts, or equivalent configurable values. They consume a typed service contract. The corresponding data service reads those values from a database or an explicitly serializable object store.

Service lifetime does not change this rule. Singleton, scoped, and transient services must use the same data boundary.

## PublisherStudio implementation

`PanelStudioTextService` consumes `IPanelStudioTextPatternDataService`. `PanelStudioTextPatternDataService` loads the reviewed seed from `Configuration/panel-text-patterns.json` and can apply a user-local serializable override under LocalApplicationData. Pattern text, options, and timeouts stay outside the panel service and components.

The seed store is copied to build and publish output. Missing or invalid required entries fail closed during service initialization rather than falling back to hardcoded service values.

## Safeguards

`build/Assert-RuntimeValueOwnership.ps1` uses the final19 declaration inventory as a removal-only maximum. New component/controller/service-owned runtime fields, properties, constants, or generated regex declarations fail validation. Existing baseline debt may be removed but must not grow.



The protected-file check, security preservation, and runtime ownership checks run from the local and release PowerShell entry points and from direct MSBuild guard targets after the existing 1-Wire architecture check.
