# PublisherStudio 2.0.2 repair candidate

See `CHANGELOG-v2.0.2.md` and `docs/architecture/task-ledger.md`.

PublisherStudio 2.0.2 restores the existing `winx64` / `setupwinx64` launcher and release layout while replacing the unsafe installer behavior with validated, rollback-capable file merges. Application updates never delete or move the installed runtime directory. Unknown files and locally modified former release files are preserved; stale files are removed only when their current hash still matches the previous schema-2 release manifest. Setup remains replaceable through its stable folder and carries a launcher-promoted standalone repair executable. Windows application archives stage that repair prelude before application files, so existing launchers can heal even when the legacy updater encounters a locked running application payload.

The normal application remains standalone. Existing installations keep their installed runtime architecture, setup self-replacement recognizes the exact stable setup folder, and no automatic recursive legacy `MediaHost` cleanup is performed. LocalGPT discovery is optional, automatic 1-Wire transport connection is disabled by default, and the default PublisherStudio loopback port remains 58071. Wire-protocol package authority remains in LocalGPT and this release consumes package 2.1.1.

The JavaScript/source-contract suite, architecture audit, JSON/XML parsing and archive/source hygiene are validation targets for this package. Native .NET 10, Razor, licensed DevExpress, Windows self-update, browser capture and hardware acceptance remain mandatory on Michael's build/test machine before publishing binaries.
