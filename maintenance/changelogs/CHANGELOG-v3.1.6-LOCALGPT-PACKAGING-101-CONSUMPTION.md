# PublisherStudio 3.1.6 - LocalGPT packaging 1.0.1 consumption

- Updated PublisherStudio to consume `LocalGPT.ReleasePackaging` 1.0.1 from the authoritative LocalGPT package source/cache.
- The 1.0.1 helper closes TAR.GZ/DEB output streams before final file moves, fixing the Windows-hosted Linux packaging sharing violation seen after PublisherStudio successfully published its linux-x64 payload.
- PublisherStudio continues not to own a duplicate release-packaging source project; the helper remains shared from LocalGPT in the same ownership model as the 1-Wire NuGet package.
- Preserved the Windows setup, Linux Full/Light native package, and macOS Full/Light package matrix.
- No editor, recording, export, localization, Panel Studio, or InteractiveServer runtime behavior was changed.
