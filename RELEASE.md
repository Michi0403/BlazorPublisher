# PublisherStudio 3.9.6

PublisherStudio 3.9.6 repairs the async-only maintenance architecture introduced in 3.9.5.

The 3.9.5 scanner overreached by declaring every synchronous component/service method invalid, requiring asynchronous disposal even when no asynchronous resource existed, rejecting short atomic/lock state protection, and lexically misclassifying domain `.Result` values and async lambdas. Converting those findings mechanically would have damaged framework, DevExpress, process/native, and synchronous adapter contracts.

The corrected build gate validates asynchronous boundaries instead: `Async` naming must match an awaitable contract, `async void` is rejected, getters may not initiate async work, renderer/awaitable paths may not block on Tasks, prohibited blocking coordination remains rejected, and direct discarded asynchronous work remains forbidden. Pure synchronous helpers and required synchronous framework/native contracts remain valid.

The corrected gate passes all 228 reviewed PublisherStudio source files. The application-architecture, async-continuation, and Razor-maintenance source audits also pass. No PublisherStudio runtime behavior was intentionally rewritten and no .NET build was attempted in this environment.

See `CHANGELOG-v3.9.6-ASYNC-BOUNDARY-ARCHITECTURE-REPAIR.md` and `VALIDATION-v3.9.6-source.md`.
