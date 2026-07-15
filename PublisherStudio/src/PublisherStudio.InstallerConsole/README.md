# PublisherStudio InstallerConsole

The installer intentionally stays small. It can copy a previously published web payload, launch it, remove it with an explicit `--force`, or download/build a source ZIP without Git.

```powershell
dotnet run --project . -- install --payload ..\..\artifacts\payload --force --start
dotnet run --project . -- start
dotnet run --project . -- uninstall --force
dotnet run --project . -- source --source-zip https://github.com/OWNER/REPO/archive/refs/heads/main.zip --force --start
```
