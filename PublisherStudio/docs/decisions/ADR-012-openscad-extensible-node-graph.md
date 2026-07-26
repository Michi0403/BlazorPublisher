# ADR-012: Extensible OpenSCAD node graph

**Status:** Accepted

OpenSCAD documents are public node graphs with catalog-driven typed parameters. Code generation dispatches through multi-registered `IOpenScadNodeRenderer` services. Animation targets node IDs and uses `$t`. A future visual builder must edit this graph rather than introduce a separate closed model.
