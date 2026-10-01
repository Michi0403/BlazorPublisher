# PublisherStudio 4.0.1

PublisherStudio 4.0.1 turns the confirmed 4.0.0 RichEdit race fix into a repository-wide InteractiveServer transient-state ownership contract.

The audit found one additional vendor control with the same feedback-loop shape: Media Studio's audio `DxRangeSelector` sent every handle movement to the server while its selected range was also supplied from component state. It now commits on handle release, so DevExpress owns the live drag and PublisherStudio receives the durable range at the interaction boundary. The already-correct Publication Timeline range selector remains unchanged.

Story/Text Studio keeps the 4.0.0 one-way document/selection ownership repair. A new build-breaking guard now rejects future two-way complex-editor live state and server-round-tripped range-handle movement, while also protecting PublisherStudio's reviewed browser coalescer for the native live-preview sliders that intentionally remain continuous.

No .NET build was attempted in this environment. See `CHANGELOG-v4.0.1-TRANSIENT-UI-STATE-OWNERSHIP.md` and `VALIDATION-v4.0.1-source.md`.
