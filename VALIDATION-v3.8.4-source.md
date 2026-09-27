# PublisherStudio 3.8.4 source validation

Source-only validation performed for this handoff:

- Python syntax check for `build/audit_razor_maintenance_contract.py`.
- The new audit was executed directly and confirmed to enumerate current repository violations with file/line/column diagnostics rather than stopping at the first source finding. A non-zero result is expected until those components are repaired in the next pass.
- Static inspection confirmed the new MSBuild target is ordered after DevExpress component retention and before the existing Razor component-attribute guard.
- Static inspection confirmed the DevExpress retention guard protects the new maintenance-target/script wiring.
- The localization identical-text baseline now uses a PowerShell-5.1-safe hashtable membership path, and Razor component-root discovery uses an explicit generic list instead of scalar-sensitive pipeline materialization.
- PublisherStudio Web, installer, package root and active module cache-busters advance to 3.8.4; DevExpress/DevExtreme remains 25.2.10.
- ZIP integrity is checked after packaging.

No `dotnet`, MSBuild, NuGet, publish, installer, or GitHub operation was run.
