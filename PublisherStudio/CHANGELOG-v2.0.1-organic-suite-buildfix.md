# PublisherStudio 2.0.1 organic-suite build fix

## Purpose

This is a narrow compiler correction over the complete 2.0.0 PublisherStudio organic-suite workspace. Existing editor, media, localization, Story Editor, frontend-authority and 1-Wire functionality is retained.

## Corrected compiler diagnostics

- Added `System.Text.Json` to `OrganicPlugins.razor`, resolving the missing `JsonValueKind` symbol in the 1-Wire round-trip test.
- Retained the earlier `Localization.razor` injection rename and structured logger/notifier integration.
- Normalized nullable import filenames before dispatching to the project-format adapters, removing the reported nullable filename warnings without changing format behavior.
- Normalized delimited text input once before indexing it, removing the reported nullable dereference warning.

## Version and protocol continuity

- PublisherStudio Web, installer, browser runtime metadata and streaming capability version advance to `2.0.1`.
- The authoritative `LocalGPT.WireProtocolVersion` dependency remains package `2.0.0`, matching LocalGPT protocol compatibility `2.0`.
- The bundled package remains available for offline debugging; release builds can continue downloading the authoritative LocalGPT release asset.

## Validation performed here

- Complete PublisherStudio npm/source-contract suite: passed after regenerating workspace evidence.
- C# composition-root, namespace, Razor flow, interpolation and project-closure source checks: passed.
- Organic plugin, Story Editor, frontend confirmation, media interaction, localization and runtime bootstrap source checks: passed.
- XML/JSON/project-reference/archive checks are recorded in the delivery report.

## Build truth

A native .NET 10/DevExpress build was unavailable in this environment. This package is intended for immediate Visual Studio rebuilding and runtime debugging.
