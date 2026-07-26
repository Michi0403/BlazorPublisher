# PublisherStudio v1.0.88 release

See `CHANGELOG-v1.0.88.md`, `SOURCE-CHANGES-v1.0.88.txt`, `TEST-RESULTS-v1.0.88.txt`, `docs/architecture/interaction-stacking-and-notification-v1.0.88.md` and `docs/architecture/task-ledger.md`.

v1.0.88 is the interaction-hardening publish candidate on top of v1.0.87. It fixes the Panel Studio drop/circuit failure, restores normal Mainframe arrangement ownership for embedded HTML/Canvas/3D web content, converges pointer/touch/keyboard/gamepad commands on shared layout semantics, replaces identified global overlay z-index escalation with local stacking contexts, and adds structured frontend logging plus a shared user-notification host.

Application and installer version is `1.0.88`. Publication format remains `1.55`. Repository contract, generated/runtime JavaScript, JSON, XML and clean-archive validation are recorded in the test report. Native .NET 10, Razor and licensed DevExpress compilation plus physical Steam Deck/touch acceptance remain mandatory on the developer workstation before publishing binaries.
