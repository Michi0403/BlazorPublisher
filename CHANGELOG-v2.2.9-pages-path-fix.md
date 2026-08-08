# PublisherStudio 2.2.9

- Corrected the GitHub Pages snapshot archive path in `Directory.Build.targets` to explicitly cross the repository directory boundary before `.github`.
- This mirrors the real-build fix discovered in LocalGPT and prevents a successful, long-running documentation build from failing only at the final snapshot-seeding step.
- No DocFX layout, API generation, Kawaii styling, PDF generation, or viewer behavior was changed.
