# PublisherStudio 3.4.4 source validation

Static/source validation for the artifact-local notarization repair. This environment cannot execute macOS signing/notarytool or the .NET release build.

Required invariants mirror LocalGPT 3.8.2: one submit per artifact transaction, pre-submit pending state, history reconciliation, hash-bound resume, and no blind resubmission.
