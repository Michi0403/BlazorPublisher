# PublisherStudio 4.9.0 — documentation UML module repair

Source candidate; release validation pending.

- Roll version from 4.8.9 to 4.9.0 in application and aligned installer console metadata and update stale documentation version metadata.
- Resolve the actual Mermaid ES module emitted by the existing DocFX build, not the previously fixed content-hashed filename. Resolve its URL relative to the page's injected theme script; retain existing rendered diagrams and clipping recovery; reset a rejected module loader promise.
- Refresh the injected theme reference on previously processed pages and validate any declared Mermaid module target using the existing Pages artifact link gate. No external CDN or new runtime dependency.
- Keep existing documentation styles, theme and application architecture.

JavaScript syntax checked with Node CLI. Source ZIP integrity checked at packaging. PowerShell, actual DocFX/browser/PDF diagrams, .NET compile and release gates are not run here. This repair addresses the identified stale-module reference, not a verified rendering outcome. Run the existing owner-side gates before deployment.
