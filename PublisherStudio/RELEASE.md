# PublisherStudio 2.0.3 build-policy compatibility repair

See `CHANGELOG-v2.0.3.md` and `docs/architecture/task-ledger.md`.

PublisherStudio 2.0.3 repairs the Windows build gates exposed by the first 2.0.2 maintainer build without disabling them. The shared architecture audit now separates child-process output from its numeric exit code, the iterator parser no longer mistakes primary-constructor type declarations for methods, and the actual uncovered iterator methods were converted to logged materialized results. Application and setup publish profiles now explicitly own Release configuration, runtime, filesystem protocol, platform and output paths. Installer launch-profile validation enumerates exact workflow names in a Windows PowerShell 5.1-compatible way.

The 2.0.2 installer/runtime preservation contract remains unchanged: established `winx64` / `setupwinx64` paths stay valid, setup can repair and replace itself, and the application directory is updated by a staged manifest-validated file merge with rollback rather than deletion or wholesale replacement. LocalGPT/1-Wire remains optional and protocol version 2.1.1 remains independently pinned.
