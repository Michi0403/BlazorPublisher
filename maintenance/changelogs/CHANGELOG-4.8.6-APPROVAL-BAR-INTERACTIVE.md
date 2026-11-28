# PublisherStudio 4.8.6

- Make the 1-Wire approval bar interactive. It sits in the statically rendered layout and had no render mode, so Approve, Decline and Hide did nothing and the bar only appeared after a page reload.
- Give the editor-session bridge the same interactive boundary, so open editor sessions are registered for `publisher.sessions.list` and `publisher.document.create`.
- Run a read-only capability that asks for no confirmation and has no saved peer rule without an approval step. `publisher.sessions.list` no longer ends as "pending approval".
- `Assert-InteractiveServerRenderModes.ps1` lists the two layout islands as reviewed render boundaries.

Not built here. Version raised from 4.8.4 to 4.8.6 in both project files, the documentation sources and the script cache key (4.8.5 was committed without a version change).

## Check in Edge
- [ ] A LocalGPT request for the session list returns sessions without an approval.
- [ ] A create request shows the approval bar without reloading; Approve once, Decline and Hide bar react.
