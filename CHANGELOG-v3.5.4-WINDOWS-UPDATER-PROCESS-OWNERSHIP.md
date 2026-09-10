# PublisherStudio 3.5.4 — Windows updater process ownership repair

- Stops only the installed `%LOCALAPPDATA%\\PublisherStudio\\win*\\PublisherStudio.Web.exe` process before update extraction so Windows file locks cannot leave a partial runtime.
- Uses `server.json` executable identity and live process executable paths only as ownership evidence; alternate/debug hosts are not terminated.
- Removes the installed runtime endpoint only after ownership is established.
- Keeps the detached setup bootstrap so `setupwin*` can still replace itself safely.
- Preserves the 3.5.3 frontend, Gallery, invariant range-binding, and macOS package validation repairs.
