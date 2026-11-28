# PublisherStudio 4.8.0 — family protocol architecture guard repair

- Corrected the 4.7.9 source regression exposed by the real Windows Debug build: `FamilyWireJsonConventions` was a static class under `Services/OrganicPlugins`, violating logging integrity, service architecture, and application-static-policy checks.
- Moved the shared JSON option conventions into the established `BusinessObjects` root as a **stateless non-static configuration type**. Both the 1-Wire codec and MVC HTTP setup use its instance methods. There is no fake operational logging or guard exemption, and existing strict service/runtime policies remain intact.
- Adjusted the family protocol PowerShell source guard to verify the non-static type and its supported location, preventing the same static helper regression from being accepted again.
- Preserved property-case-insensitive reads, string-enum compatibility, numeric-string **input** tolerance, existing outgoing numeric representation and signed-envelope formatting. No 1-Wire wire-format/schema, transport, pairing, MFA, SignalR, MessagePack, installer implementation or inference code was changed.
- Advanced PublisherStudio Web and Setup project versions together from 4.7.9 to 4.8.0.

**Build evidence:** user-supplied Windows Debug log showed the three independent guard failures on 4.7.9. This revision was checked against those same guard patterns and source contracts but could not be built or execute PowerShell guards in this preparation environment. Windows/macOS/Linux compilation and runtime verification remain necessary.
