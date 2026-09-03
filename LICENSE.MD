# License and third-party notice

## Project license

PublisherStudio project-owned source is provided under the Apache License, Version 2.0.

Copyright 2026 Michael Fleischer, Germany.

The Apache-2.0 grant applies to source and documentation owned by this project unless a file states otherwise. It does not relicense third-party software, assets, services, trademarks, packages, or generated proprietary material. See the repository `LICENSE` file for the full Apache-2.0 text.

## DevExpress boundary

PublisherStudio uses proprietary DevExpress components. DevExpress is not licensed under Apache-2.0 and this repository grants no DevExpress license.

Building or modifying the maintained DevExpress-based UI requires an appropriately licensed DevExpress development environment under Developer Express Inc.'s terms. The current repository restores .NET packages through NuGet.org (plus the local LocalGPT wire-protocol cache where applicable); the private DevExpress developer license identity is a separate build-time requirement. Browser asset preparation likewise uses the declared npm packages and DevExpress's license-generation tooling.

The private DevExpress developer license, credentials, `node_modules`, package caches, and personal build data are not redistributed in the source package. A licensed build may produce application/runtime assets that are redistributable when the producing developer's applicable DevExpress terms permit it. End-user installations do not receive the private developer license.

Rights and obligations for building, using, or redistributing DevExpress components are determined solely by the applicable DevExpress license terms. This project notice does not add a new DevExpress requirement beyond those terms.

## Other third parties

Microsoft .NET/browser platform components and every other third-party library, executable, asset, service, codec, or package remain governed by their own terms. No third-party dependency becomes Apache-2.0 merely because PublisherStudio integrates it.

See `THIRD-PARTY-NOTICES.md` for the maintained dependency summary.
