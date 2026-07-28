# BlazorPublisher 2.0.1 final24

## Fixed

- Corrected the PowerShell 5.1 array syntax in `Assert-ProtectedArchitectureFiles.ps1`: the missing separator after `appsettings.json` was restored and the unsupported trailing comma before the closing parenthesis was removed.
- Refreshed only the reviewed protected-architecture hash for that script.

## Preserved

- JavaScript diagnostics, Panel Studio interaction lifecycle rules, final19 security preservation, 1-Wire architecture rules, and runtime-value ownership were not weakened.
