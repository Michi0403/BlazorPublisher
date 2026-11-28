# PublisherStudio

PublisherStudio is an open-source, local-first .NET 10 and Blazor publishing environment for documents, stories, pictures, spreadsheets, presentations, websites, video, streaming, and reusable interactive panels. It is designed to remain useful as a standalone application while optionally cooperating with LocalGPT and organic 1-Wire peers.

[Latest release](https://github.com/Michi0403/BlazorPublisher/releases/latest) · [User guide](https://michi0403.github.io/BlazorPublisher/) · [Release history](maintenance/changelogs/)

## Install and update

Download the package for your platform from the [latest release](https://github.com/Michi0403/BlazorPublisher/releases/latest). On macOS, open the `.pkg` and follow Installer, or open the DMG and drag PublisherStudio to Applications. Launch it from Applications. See the [installation guide](docs/articles/installer-and-updates.md) for platform-specific steps.

PublisherStudio can be used on its own. See [LocalGPT and 1-Wire](docs/articles/localgpt-and-onewire.md) to connect the applications and use PublisherStudio tools from LocalGPT.

## Future2 role

PublisherStudio is the creative/productivity side of the broader **Future2** direction: user-owned software and AI infrastructure that does not require a centralized corporate or government service to own the user's data, tools, or workflow. Cloud and web services can be integrated where useful, but local operation and human authority remain first-class.

Together, LocalGPT and PublisherStudio demonstrate that the same open .NET foundation can cover both AI orchestration and ordinary creative/productivity software. The intended scale ranges from one person's workstation to independently operated local infrastructure, with explicit interfaces for cooperating systems rather than mandatory platform lock-in.

## Build

The solution is `src/PublisherStudio.sln`. Generated/licensed DevExpress browser assets are intentionally excluded from clean source packages. The maintained build entry points have both Windows `.cmd` wrappers and cross-platform PowerShell scripts.

```powershell
# Windows
.\Build-LocalDevelopment.ps1

# macOS / Linux
pwsh ./Build-LocalDevelopment.ps1

# Owner/release lane
pwsh ./Build-Release.ps1
```

The development and release scripts restore the pinned .NET tools from both the root and application manifests before compilation. To refresh the workloads for the active SDK as part of a build, pass `-DotNetWorkloads Update`. If your SDK installation requires administrator access on macOS or Linux, use `-DotNetWorkloads UpdateWithSudo`. Only the workload update runs through `sudo`.

The PowerShell entry points run the repository's architecture and Pages maintenance checks and invoke the managed release-packaging helper. Normal builds validate and provision the pinned DevExpress/DevExtreme browser assets and runtime key before compilation. A licensed development/build environment is required for the maintained DevExpress-based UI. Current .NET package restore uses NuGet.org plus the repository-local LocalGPT wire-protocol cache where applicable; the private DevExpress developer license is a separate build-time identity and is not included in source ZIPs or end-user installations.

## Documentation

The maintained Markdown is under `docs/`. A normal owner build creates one Kawaii DocFX tree in `wwwroot/help-docs`, including the complete XML-generated API reference and the versioned HTML-backed PDF. `Update-GitHubPagesSnapshot.cmd` / its PowerShell counterpart validates that generated tree and refreshes `.github/pages/publisherstudio-kawaii-docs.zip`. GitHub Pages publishes that exact validated snapshot; the authored `docs/` directory is never overwritten with generated output.

The in-app Help page opens the same HTML, PDF, API reference, and status routes in the focus-managed documentation viewer.

## License

PublisherStudio project-owned source is Apache-2.0. The maintained UI uses proprietary DevExpress components under DevExpress's separate terms. The repository grants no DevExpress license and does not ship the private developer license. See `LICENSE`, `LICENSE.MD`, and `THIRD-PARTY-NOTICES.md` for the exact project/third-party boundary.

## Provenance and acknowledgments

PublisherStudio was created and is maintained by **Michael Fleischer (`Michi0403`)**.

The project also reflects repeated co-development sessions with OpenAI's ChatGPT and Anthropic's Claude, and many LocalGPT Council reviews and missing-feature reports that were inspected and used as engineering input.
Claude contributed from 4.7.3 onward: the 1-Wire traffic journal with its configurable logging and live in-app traffic view.
Assistance is acknowledged openly; design decisions, repository ownership, release decisions, and responsibility remain with the maintainer.
