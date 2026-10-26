# PublisherStudio 4.6.1 verification

- 21/21 Node.js source/fixture test files passed.
- 17/17 normalized maintained JavaScript hashes match `build/javascript-diagnostics-files.sha256`.
- Kept JavaScript diagnostics gate and notifier bridge unchanged and enforced.
- No PowerShell, NuGet restore, or .NET build was run; the Visual Studio NuGet restore warning remains unverified.
