# PublisherStudio 4.2.1 — five Windows Debug maintenance failures repaired

Source baseline: complete PublisherStudio 4.2.0 archive, itself derived from PublisherStudio 4.1.9. This patch changes **no application C#, Razor, JavaScript, CSS, SQL, assets, or existing service methods**. The two application project versions become 4.2.1. LocalGPT is unchanged.

## One-command migration and unchanged policy enforcement

1. **Leftover Python file after overlay extraction:** `Assert-MaintenanceArchitecture.ps1` invokes the new PowerShell migration for the exact 132 retired 4.1.9 maintenance paths listed in `build/obsolete-python-maintenance.sha256`. It moves any leftover files into `artifacts/.build-storage/retired-python-maintenance`, preserving local edits and backups. The pre-existing strict scan then **still rejects every remaining `.py`** and missing PowerShell contract. Unknown Python is not exempted. This is automatic on the normal Visual Studio, Debug, and Release entrypoints; no independent migration command.
2. **Windows PowerShell 5.1 root binding:** 11 guards now resolve the script-relative repository root inside the executable script body, rather than evaluating `$PSScriptRoot` as a parameter default. Callers may still override `-RepositoryRoot`.
3. **Editor pre-render JS:** `Assert-ComponentSafety.ps1` inspects balanced `OnInitialized`/`OnInitializedAsync` method bodies, not a 1,200-character lookahead into neighboring methods. JS calls directly inside initialization remain forbidden. The existing legitimate `OnAfterRenderAsync` interop remains unchanged.
4. **Controller method diagnostics:** `Invoke-ArchitectureAudit.ps1` masks single-line/XML comments with equal-length spaces. This preserves character offsets when extracting method bodies, so `RuntimePolicyController.Get()`'s existing `try/catch` and logged errors are audited correctly.
5. **Following gates and integration:** the architectural checks, strict component retention policy, native async/iterator/diagnostics rules, and MSBuild guard wiring remain enabled. All 4.2.0 source files survive unchanged except for the listed guard fixes and version fields. Other param-default copies were fixed proactively to avoid repeating the same failure later.

## Acceptance

Open `src/PublisherStudio.sln` in Visual Studio and perform the normal Debug build. No separate preparation command. `Build-LocalDevelopment.ps1` and `Build-Release.ps1` likewise retain their one-command contract. The five reported failures were inspected and their source conditions corrected. **The actual Visual Studio/.NET/PowerShell build was not run in this Linux packaging environment.**
