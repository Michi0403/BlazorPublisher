# PublisherStudio 3.6.0 source validation

Validation is source/static because the assistant environment does not provide the user's macOS signing identities, Apple notarization account, Microsoft Edge macOS process behavior, PowerShell, or the .NET SDK. No `dotnet build`, `codesign`, notarization submission, GitHub access, or native package execution is claimed.

Validated statically:

- PublisherStudio web and installer projects plus frontend package identities report 3.6.0 and obey the one-digit minor/patch policy;
- `build/assets/mac-apphost-entitlements.plist` parses as a property list and contains only `com.apple.security.cs.allow-jit = true`;
- macOS trust preflight requires `plutil` and lints that exact asset before expensive release work;
- native Developer ID signing normalizes the checked-in asset to XML1, lints the temporary copy, and applies it only to the .NET apphost;
- the previous inline PowerShell entitlement here-string is absent;
- browser PDF rendering detects stable complete output before process exit, while the 480-second value remains a real timeout;
- the maintained logging baseline is unchanged and the shared single-writer logging design remains present;
- the LocalGPT-aligned overlay installer contract and early installer compile preflight remain present;
- the 3.5.9 XML documentation parameter/lifetime repair remains present;
- maintained application architecture, async continuation, component resilience, prerender interop, service resilience, text ownership, iterator, system-variable, Panel Studio and related Python audits pass.

The user's macOS `pwsh Build-Release.ps1` run remains the authoritative execution test for `plutil`, browser-process completion, Developer ID signing, notarization, and native package production.
