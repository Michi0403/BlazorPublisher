# PublisherStudio 4.6.7 — Restore connection-gated LocalGPT AI visibility

Base: complete PublisherStudio 4.6.6 source archive supplied in the conversation. This is an offline source-only patch; no GitHub or .NET build was used.

## Changes

- Show the LocalGPT AI ribbon and media/team commands, Story Editor AI tab, AI Text quick insert, Panel Library AI request, and selected-object context-menu assistance only when `State.IsConnected && State.IsLinked` (live TCP session with reciprocal frontend approval). Neither a TCP-only connection nor an offline application reveals these entry points.
- Keep AI entry points visible during a transient `council.run` capability refresh **while the reciprocal connection remains active**. Capability validation on actual AI sends is retained; visibility must never grant execution rights.
- Recheck the link before accepting an already queued AI menu callback; prevent a stale click after disconnection from launching a new AI authoring dialog.
- Keep existing user-opened AI review panels/proposals intact if connection subsequently drops; permission-guarded submission remains unavailable until restored.
- Keep the regular Chat editor unchanged. Show the optional AI Chat quick insert/ribbon action only with an active LocalGPT link **or** a separately configured Direct OpenAI model/key; this preserves legitimate OpenAI-only use while hiding AI clutter for users with neither provider. Leave Organic Plugins/1-Wire connection settings accessible while disconnected.
- Preserve all 4.6.6 public pairing ticket and authenticated current-session capability directory fixes without changing trust, MFA seeds or protocol rules.
- Add source-contract regression tests for offline/unauthenticated visibility, linked capability-refresh visibility, queued action guards, and existing execution protection; update the earlier 4.6.6 guard that incorrectly expected unconditional visibility.
- Increase Web and InstallerConsole project versions from 4.6.6 to 4.6.7.

## Validation scope

- Offline source and JavaScript contract tests only; .NET compilation and UI/1-Wire runtime integration are not performed.
