# PublisherStudio 3.0.4 — Native Discovery Compile and Platform Warning Repair

- Restored the missing `PublisherStudio.BusinessObjects` import required by the host-specific native capture discovery implementations so `PublisherRuntimePattern` resolves during compilation.
- Hardened documentation cache path comparison against a nullable cached path.
- Guarded Unix file-mode application inside the Unix platform implementation so the .NET platform analyzer can prove the Windows path is excluded.
- No render-mode, GitHub Pages, DevExpress asset, or application feature behavior was intentionally changed.
