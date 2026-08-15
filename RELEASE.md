# PublisherStudio 2.7.3 logging and recording recovery

PublisherStudio 2.7.3 restores the persistent application file logger and hardens Video/Audio Studio recording finalization across Blazor disconnect/reconnect boundaries. A completed browser recording is retained before capture hardware is released and can be recovered by a newly attached circuit. Logging state lives in BusinessObjects and the DI implementation lives under Services/Logging; Razor helper statics were converted to instance members.

## Versions

- PublisherStudio.Web: 2.7.3
- PublisherStudio.InstallerConsole: 2.7.3
- LocalGPT Wire Protocol package: 2.1.1 (unchanged)

Default application log: `%LocalAppData%\PublisherStudio\PublisherStudio.log`.

See `CHANGELOG-v2.7.3-LOGGING-RECORDING-RECOVERY.md` and `VALIDATION-v2.7.3-source.md`.
