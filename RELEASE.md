# PublisherStudio 2.9.7

PublisherStudio 2.9.7 is the **Build and Architecture Compliance Repair** release.

It retains the complete 2.9.6 signal/media/template-library feature set while addressing the two defects exposed by the authoritative Windows build: the New-from-template component now satisfies the repository catch/log/user-notification boundary, and Panel Studio no longer declares colliding `templateId` locals that produced CS0136.

The source targets .NET 10 and DevExpress/DevExtreme 25.2.9. This archive is **SOURCE-NOT-COMPILED** in the preparation environment; the user's licensed Windows build remains authoritative.

See `CHANGELOG-v2.9.7-BUILD-ARCHITECTURE-COMPLIANCE-REPAIR.md` and `VALIDATION-v2.9.7-source.md`.
