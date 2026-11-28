# PublisherStudio 2.5.5 changelog

## LocalGPT AI Assist surface

- Reworked the linked `/organic-plugins` page into a PublisherStudio-oriented **LocalGPT AI Assist** surface instead of presenting Council/protocol administration as the primary creative workflow.
- Added a simple `Create with LocalGPT` card with target, prompt and an Auto/Text/Spreadsheet/3D-OpenSCAD/Project-source profile.
- The current PublisherStudio selection/page is used to prefill target/context and to infer a sensible profile where possible.
- Team/preset override remains available but is secondary/expandable and is sourced from the currently advertised LocalGPT Council capabilities rather than a duplicated hard-coded Publisher-only team configuration.
- Custom Council runs, protocol details, negotiated routes, trust/security controls and capability/permission maintenance remain available behind advanced expandable sections.
- Simplified Publisher ribbon and Story Editor wording to `LocalGPT AI`, `AI Assist selected object`, and text-generation/proposal language.

## Export behavior

- AI-generated text remains reviewable before insertion. Once inserted into a PublisherStudio document/component it is ordinary publication content and therefore follows the normal HTML/export pipeline.
- This release does not expose an unauthenticated LocalGPT loopback endpoint to arbitrary exported websites. Live AI invocation remains a linked/trusted PublisherStudio ↔ LocalGPT concern; baked publication content remains portable.

## 1-Wire round-trip and dynamic capability handling

- Fixed the Organic connection round-trip test: `CapabilityResponse`, `SkillResponse` and `SkillStateUpdate` now satisfy the waiting correlation just like work/error/approval replies, so a `CapabilityRequest` no longer times out despite receiving its valid response.
- Retained automatic post-link capability synchronization from 2.5.4: catalog/permission changes are rebuilt and broadcast over the existing link, and LocalGPT can also request the current capability/skill directory explicitly.
- Route/capability details used by AI Assist continue to come from the negotiated 1-Wire surface instead of assuming a fixed LocalGPT URL/API set.

## Viewport and PublisherStudio styling

- Fixed the Organic/AI Assist page being clipped by the application root's fixed viewport/hidden overflow. The page now owns its vertical scroll area and remains usable at normal desktop viewport heights.
- Added Publisher-native card/detail styling and moved rarely needed operational controls behind collapsible advanced sections.

## Existing editor fixes retained

- Retained Panel Studio queued-interaction flushing before reusable module/panel snapshots and the 2.5.4 async-continuation policy corrections.
- Retained working Panel Studio geometry/canvas behavior rather than rewriting the editor.

## Version

- PublisherStudio Web/installer: **2.5.5**.
